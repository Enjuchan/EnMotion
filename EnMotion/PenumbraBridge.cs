using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace EnMotion;

/// <summary>
/// Fragt Penumbra, welche Dateien es ersetzt.
///
/// Die Schnittstelle wird ueber die IPC-Namen angesprochen statt ueber eine Referenz
/// auf Penumbra.Api - das erspart eine Abhaengigkeit, die sich mit jeder
/// Penumbra-Version aendern koennte. Die Namen stammen aus Penumbra.Api,
/// Verzeichnis IpcSubscribers.
/// </summary>
public static class PenumbraBridge
{
    private const string GetCollectionForObjectLabel = "Penumbra.GetCollectionForObject.V5";

    private const string GetChangedItemsForCollectionLabel = "Penumbra.GetChangedItemsForCollection";
    private const string GetModListLabel = "Penumbra.GetModList";
    private const string GetChangedItemsLabel = "Penumbra.GetChangedItems.V5";
    private const string ResolvePlayerPathsLabel = "Penumbra.ResolvePlayerPaths";

    // Ereignisse (Penumbra.Api, IpcSubscribers/ModSettings.cs, Mods.cs, PluginState.cs).
    // Eine Ereignis-Schnittstelle fuer Aenderungen der zugewiesenen Sammlung gibt es dort nicht.
    private const string ModSettingChangedLabel = "Penumbra.ModSettingChanged.V5";
    private const string ModAddedLabel = "Penumbra.ModAdded";
    private const string ModDeletedLabel = "Penumbra.ModDeleted";
    private const string ModMovedLabel = "Penumbra.ModMoved";
    private const string ModDirectoryChangedLabel = "Penumbra.ModDirectoryChanged";
    private const string EnabledChangeLabel = "Penumbra.EnabledChange";
    private const string InitializedLabel = "Penumbra.Initialized";
    private const string DisposedLabel = "Penumbra.Disposed";

    // Ticks des letzten Ereignisses, 0 ohne offene Aenderung. Die Ereignisse kommen nicht
    // zwingend vom Zeichen-Thread, deshalb nur Interlocked.
    private static long changeTicks;
    private static readonly List<Action> unsubscribers = [];

    // In EnEmotes kam das Plugin-Interface aus Umbras Framework, weil dessen
    // Dienst-Container es nicht fuehrt. Hier haengt es direkt am Plugin.
    private static IDalamudPluginInterface Api => EnMotion.Plugin.PluginInterface;

    /// <summary>Abonniert die Ereignisse, nach denen sich die aktiven Mods geaendert haben koennen.</summary>
    public static void Subscribe()
    {
        // Das Abonnement steht auch ohne geladenes Penumbra, die Ereignisse kommen dann spaeter.
        // Das erste Argument von ModSettingChanged ist ein Enum aus Penumbra.Api; hier genuegt der int-Wert.
        Listen<int, Guid, string, bool>(ModSettingChangedLabel, (_, _, _, _) => Mark());
        Listen<string>(ModAddedLabel, _ => Mark());
        Listen<string>(ModDeletedLabel, _ => Mark());
        Listen<string, string>(ModMovedLabel, (_, _) => Mark());
        Listen<string, bool>(ModDirectoryChangedLabel, (_, _) => Mark());
        Listen<bool>(EnabledChangeLabel, _ => Mark());
        Listen(InitializedLabel, () => Mark(immediate: true));
        Listen(DisposedLabel, () => Mark());
    }

    public static void Unsubscribe()
    {
        foreach (var unsubscribe in unsubscribers)
            unsubscribe();

        unsubscribers.Clear();
    }

    private static void Listen(string label, Action action)
    {
        var gate = Api.GetIpcSubscriber<object>(label);
        gate.Subscribe(action);
        unsubscribers.Add(() => gate.Unsubscribe(action));
    }

    private static void Listen<T1>(string label, Action<T1> action)
    {
        var gate = Api.GetIpcSubscriber<T1, object>(label);
        gate.Subscribe(action);
        unsubscribers.Add(() => gate.Unsubscribe(action));
    }

    private static void Listen<T1, T2>(string label, Action<T1, T2> action)
    {
        var gate = Api.GetIpcSubscriber<T1, T2, object>(label);
        gate.Subscribe(action);
        unsubscribers.Add(() => gate.Unsubscribe(action));
    }

    private static void Listen<T1, T2, T3, T4>(string label, Action<T1, T2, T3, T4> action)
    {
        var gate = Api.GetIpcSubscriber<T1, T2, T3, T4, object>(label);
        gate.Subscribe(action);
        unsubscribers.Add(() => gate.Unsubscribe(action));
    }

    // Bei Initialized sofort faellig: Tick 1 liegt weit vor jeder Ruhezeit.
    private static void Mark(bool immediate = false) =>
        Interlocked.Exchange(ref changeTicks, immediate ? 1 : DateTime.UtcNow.Ticks);

    /// <summary>
    /// Ob eine Aenderung vorliegt, deren letztes Ereignis mindestens <paramref name="quiet"/>
    /// zurueckliegt. Nimmt die Markierung dabei weg; ein Ereignis dazwischen setzt sie neu.
    /// </summary>
    public static bool TryTakeChange(TimeSpan quiet)
    {
        var ticks = Interlocked.Read(ref changeTicks);
        if (ticks == 0 || DateTime.UtcNow.Ticks - ticks < quiet.Ticks)
            return false;

        return Interlocked.CompareExchange(ref changeTicks, 0, ticks) == ticks;
    }

    /// <summary>Verwirft eine offene Markierung, wenn ohnehin gleich abgefragt wird.</summary>
    public static void ClearChange() => Interlocked.Exchange(ref changeTicks, 0);

    /// <summary>
    /// Grund des letzten Fehlschlags. Waehrend der Fehlersuche unverzichtbar: Ohne
    /// das sieht man nur ein leeres Ergebnis und weiss nicht, ob Penumbra fehlt, der
    /// Dienst nicht bereitsteht oder der IPC-Name nicht stimmt.
    /// </summary>
    public static string? LastError => lastError?.ToString();

    private static LocText? lastError;

    /// <summary>Technische Meldungen aus Ausnahmen bleiben in beiden Sprachen gleich.</summary>
    private static void Fail(Exception e) => lastError = new LocText(e.GetType().Name + ": " + e.Message, e.GetType().Name + ": " + e.Message);

    /// <summary>
    /// Was in der Sammlung des eigenen Charakters tatsaechlich veraendert wird.
    ///
    /// Das ist der entscheidende Unterschied zum Durchgehen aller Mods: Hier zaehlt
    /// nur, was auch eingeschaltet ist. Penumbra rechnet die Sammlung ohnehin aus -
    /// deaktivierte Mods tauchen darin gar nicht erst auf.
    /// </summary>
    public static Dictionary<string, object?> GetActiveChangedItems()
    {
        try
        {
            // Objektindex 0 ist der eigene Charakter. Penumbra beantwortet damit,
            // welche Sammlung fuer ihn tatsaechlich wirkt - unabhaengig davon, ob sie
            // ihm einzeln zugewiesen ist oder ueber eine allgemeine Regel greift.
            // Der Weg ueber die Sammlungs-Typen fuehrte hier ins Leere: "Yourself"
            // ist nur belegt, wenn man dem Charakter ausdruecklich etwas zuweist.
            var (valid, _, effective) = Api
                .GetIpcSubscriber<int, (bool, bool, (Guid Id, string Name))>(GetCollectionForObjectLabel)
                .InvokeFunc(0);

            if (!valid)
            {
                lastError = new LocText("no character loaded", "kein Charakter geladen");
                return [];
            }

            return Api
                .GetIpcSubscriber<Guid, Dictionary<string, object?>>(GetChangedItemsForCollectionLabel)
                .InvokeFunc(effective.Id);
        }
        catch (Exception e)
        {
            Fail(e);
            return [];
        }
    }

    /// <summary>Alle bekannten Mods als Verzeichnis-zu-Name.</summary>
    public static Dictionary<string, string> GetMods()
    {
        try
        {
            return Api.GetIpcSubscriber<Dictionary<string, string>>(GetModListLabel).InvokeFunc();
        }
        catch (Exception e)
        {
            Fail(e);
            return [];
        }
    }

    /// <summary>Die Gegenstaende, die ein Mod nach Penumbras Einschaetzung veraendert.</summary>
    public static Dictionary<string, object?> GetChangedItems(string modDirectory, string modName)
    {
        try
        {
            return Api
                .GetIpcSubscriber<string, string, Dictionary<string, object?>>(GetChangedItemsLabel)
                .InvokeFunc(modDirectory, modName);
        }
        catch (Exception e)
        {
            Fail(e);
            return [];
        }
    }

    /// <summary>
    /// Loest Spielpfade fuer den eigenen Charakter auf. Weicht das Ergebnis vom
    /// Eingabepfad ab, wird die Datei durch einen Mod ersetzt.
    /// </summary>
    public static string[] Resolve(string[] gamePaths)
    {
        try
        {
            var (forward, _) = Api
                .GetIpcSubscriber<string[], string[], (string[], string[][])>(ResolvePlayerPathsLabel)
                .InvokeFunc(gamePaths, []);

            return forward;
        }
        catch (Exception e)
        {
            Fail(e);
            return [];
        }
    }

    /// <summary>
    /// Ob Penumbra erreichbar ist.
    ///
    /// Ohne die Mod-Erkennung hat EnMotion nichts anzuzeigen - ein leeres Fenster ohne
    /// Erklaerung waere das Schlechteste, was man dem Nutzer hinstellen kann.
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                Api.GetIpcSubscriber<Dictionary<string, string>>(GetModListLabel).InvokeFunc();
                return true;
            }
            catch (Exception e)
            {
                lastError = new LocText("Penumbra not reachable: " + e.GetType().Name, "Penumbra nicht erreichbar: " + e.GetType().Name);
                return false;
            }
        }
    }
}
