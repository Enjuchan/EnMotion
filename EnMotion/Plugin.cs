using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EnMotion.Core;

namespace EnMotion;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider Textures { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    private readonly Configuration config;
    private readonly Favourites favourites = new();
    private readonly PoseFavourites poseFavourites = new();
    private readonly MainWindow window;
    private readonly ButtonWindow button;
    private readonly WindowSystem windows = new("EnMotion");

    public Plugin()
    {
        // ECommons braucht das für DalamudReflector, das der Dependencies-Tab zum Installieren nutzt.
        ECommons.ECommonsMain.Init(PluginInterface, this);

        config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Loc.Init(PluginInterface, config.Language);
        favourites.Load(config.Favourites);
        poseFavourites.Load(config.PoseFavourites);

        Ui.Widgets.Init(Textures);
        var version = PluginInterface.Manifest.AssemblyVersion.ToString(3);
        window = new MainWindow(config, favourites, poseFavourites, Speichern, version,
                                new Dependencies(PluginInterface, Log, EnMotionDependencies.Groups),
                                EnMotionAbout.Create(version), Log);
        windows.AddWindow(window);
        button = new ButtonWindow(config, window.Toggle, Speichern);

        PluginInterface.UiBuilder.Draw += Zeichnen;

        // Dalamud kennt zwei Einstiege: das Zahnrad fuer Einstellungen und den Knopf
        // fuer das Hauptfenster. Ohne OpenMainUi mahnt der Plugin-Installer das an.
        PluginInterface.UiBuilder.OpenConfigUi += EinstellungenOeffnen;
        PluginInterface.UiBuilder.OpenMainUi += Oeffnen;

        // Dalamud übernimmt den Hilfetext bei der Anmeldung, deshalb wird der Befehl beim Sprachwechsel neu angemeldet.
        BefehlAnmelden();
        Loc.Changed += BefehlAnmelden;

        // Ein Posenwechsel, der waehrend eines Emotes angefordert wurde, wird hier
        // nachgeholt - ohne diesen Puls bleibt er fuer immer liegen.
        Framework.Update += Puls;

        PenumbraBridge.Subscribe();

        // Penumbra ist zwingend: Ohne die Mod-Erkennung gibt es nichts anzuzeigen.
        if (!PenumbraBridge.IsAvailable)
        {
            Chat.PrintError($"[EnMotion] {Loc.T(
                $"Penumbra is required: {PenumbraBridge.LastError}",
                $"Penumbra wird benötigt: {PenumbraBridge.LastError}")}");
        }
    }

    private void BefehlAnmelden()
    {
        Commands.RemoveHandler("/enmotion");
        Commands.AddHandler("/enmotion", new Dalamud.Game.Command.CommandInfo((_, _) => Oeffnen())
        {
            HelpMessage = EnMotionAbout.ToggleEffect.ToString(),
        });
    }

    private void Zeichnen()
    {
        // Im Ladebildschirm wird nichts gezeichnet.
        if (Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51])
            return;

        // IsOpen bleibt unberuehrt, damit beides nach dem Kampf von selbst wiederkommt.
        if (config.HideInCombat && Condition[ConditionFlag.InCombat])
            return;

        // IsOpen wird nicht gespeichert und steht beim Start auf false: Auf dem Titelbildschirm erscheint das
        // Fenster nur, wenn der Nutzer es in dieser Sitzung selbst geöffnet hat. Der Knopf kommt erst nach dem Login.
        windows.Draw();

        if (ClientState.IsLoggedIn)
            button.Draw();
    }

    private static void Puls(IFramework _) => PoseController.Tick();

    private void Oeffnen() => window.Toggle();

    private void EinstellungenOeffnen() => window.ToggleSettings();

    private void Speichern()
    {
        config.Favourites = [.. favourites.Ids];
        config.PoseFavourites = poseFavourites.ToEntries();
        PluginInterface.SavePluginConfig(config);
    }

    public void Dispose()
    {
        Loc.Changed -= BefehlAnmelden;
        Loc.Dispose();
        Commands.RemoveHandler("/enmotion");
        windows.RemoveAllWindows();

        PluginInterface.UiBuilder.Draw -= Zeichnen;
        PluginInterface.UiBuilder.OpenConfigUi -= EinstellungenOeffnen;
        PluginInterface.UiBuilder.OpenMainUi -= Oeffnen;
        Framework.Update -= Puls;
        PenumbraBridge.Unsubscribe();

        Speichern();
        ECommons.ECommonsMain.Dispose();
    }
}
