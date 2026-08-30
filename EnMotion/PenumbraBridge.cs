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

    // In EnEmotes kam das Plugin-Interface aus Umbras Framework, weil dessen
    // Dienst-Container es nicht fuehrt. Hier haengt es direkt am Plugin.
    private static IDalamudPluginInterface Api => EnMotion.Plugin.PluginInterface;

    /// <summary>
    /// Grund des letzten Fehlschlags. Waehrend der Fehlersuche unverzichtbar: Ohne
    /// das sieht man nur ein leeres Ergebnis und weiss nicht, ob Penumbra fehlt, der
    /// Dienst nicht bereitsteht oder der IPC-Name nicht stimmt.
    /// </summary>
    public static string? LastError { get; private set; }

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
                LastError = "no character loaded";
                return [];
            }

            return Api
                .GetIpcSubscriber<Guid, Dictionary<string, object?>>(GetChangedItemsForCollectionLabel)
                .InvokeFunc(effective.Id);
        }
        catch (Exception e)
        {
            LastError = e.GetType().Name + ": " + e.Message;
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
            LastError = e.GetType().Name + ": " + e.Message;
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
            LastError = e.GetType().Name + ": " + e.Message;
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
            LastError = e.GetType().Name + ": " + e.Message;
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
                LastError = "Penumbra nicht erreichbar: " + e.GetType().Name;
                return false;
            }
        }
    }
}
