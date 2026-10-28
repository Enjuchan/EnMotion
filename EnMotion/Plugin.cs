using Dalamud.Game.ClientState.Conditions;
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

    public Plugin()
    {
        config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        favourites.Load(config.Favourites);
        poseFavourites.Load(config.PoseFavourites);

        Ui.Widgets.Init(Textures);
        window = new MainWindow(config, favourites, poseFavourites, Speichern,
                                PluginInterface.Manifest.AssemblyVersion.ToString(3));
        button = new ButtonWindow(config, () => window.Visible = !window.Visible, Speichern);

        PluginInterface.UiBuilder.Draw += Zeichnen;

        // Dalamud kennt zwei Einstiege: das Zahnrad fuer Einstellungen und den Knopf
        // fuer das Hauptfenster. Ohne OpenMainUi mahnt der Plugin-Installer das an.
        PluginInterface.UiBuilder.OpenConfigUi += Oeffnen;
        PluginInterface.UiBuilder.OpenMainUi += Oeffnen;

        Commands.AddHandler("/enmotion", new Dalamud.Game.Command.CommandInfo((_, _) => Oeffnen())
        {
            HelpMessage = "Opens EnMotion.",
        });

        // Ein Posenwechsel, der waehrend eines Emotes angefordert wurde, wird hier
        // nachgeholt - ohne diesen Puls bleibt er fuer immer liegen.
        Framework.Update += Puls;

        PenumbraBridge.Subscribe();

        // Penumbra ist zwingend: Ohne die Mod-Erkennung gibt es nichts anzuzeigen.
        if (!PenumbraBridge.IsAvailable)
            Chat.PrintError($"[EnMotion] Penumbra is required: {PenumbraBridge.LastError}");
    }

    private void Zeichnen()
    {
        // Ohne diese Pruefung stand der Knopf schon auf dem Titelbildschirm und in Ladebildschirmen.
        if (!ClientState.IsLoggedIn || Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51])
            return;

        // Visible bleibt unberuehrt, damit beides nach dem Kampf von selbst wiederkommt.
        if (config.HideInCombat && Condition[ConditionFlag.InCombat])
            return;

        window.Draw();
        button.Draw();
    }

    private static void Puls(IFramework _) => PoseController.Tick();

    private void Oeffnen() => window.Visible = !window.Visible;

    private void Speichern()
    {
        config.Favourites = [.. favourites.Ids];
        config.PoseFavourites = poseFavourites.ToEntries();
        PluginInterface.SavePluginConfig(config);
    }

    public void Dispose()
    {
        Commands.RemoveHandler("/enmotion");

        PluginInterface.UiBuilder.Draw -= Zeichnen;
        PluginInterface.UiBuilder.OpenConfigUi -= Oeffnen;
        PluginInterface.UiBuilder.OpenMainUi -= Oeffnen;
        Framework.Update -= Puls;
        PenumbraBridge.Unsubscribe();

        Speichern();
    }
}
