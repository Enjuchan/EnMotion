using Dalamud.Bindings.ImGui;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>Inhalte des Willkommensfensters: Zweck, erste Schritte und die wichtigsten Einstellungen.</summary>
public static class EnMotionWelcome
{
    public static WelcomeContent Create(string version, Configuration config, Action save, Action<ViewMode> changeView) => new(
        "EnMotion",
        version,
        new("EnMotion lists the emotes, dances, expressions and idle poses that your enabled Penumbra mods change, and plays them with one click. It is built to stay open on screen while you play.",
            "EnMotion listet die Emotes, Tänze, Mimik und Idle-Posen, die deine aktiven Penumbra-Mods verändern, und spielt sie mit einem Klick ab. Es ist dafür gebaut, beim Spielen auf dem Bildschirm offen zu bleiben."),
        [
            new("Make sure Penumbra is installed and has mods enabled. The list below shows whether it is missing and installs it for you.",
                "Stell sicher, dass Penumbra installiert ist und Mods aktiv sind. Die Liste unten zeigt, ob es fehlt, und installiert es für dich."),
            new("Open EnMotion with the small button on your screen or with /enmotion. Click a tile to play that emote.",
                "Öffne EnMotion über den kleinen Knopf auf dem Bildschirm oder mit /enmotion. Klick auf eine Kachel, um das Emote abzuspielen."),
            new("Right-click a tile to add it to your favourites; they sit at the top of every tab.",
                "Klick mit rechts auf eine Kachel, um sie zu den Favoriten zu legen; sie stehen oben in jedem Tab."),
            new("Check the settings below. More options are in the Settings tab.",
                "Prüfe die Einstellungen unten. Weitere findest du im Settings-Tab."),
        ],
        width => DrawSettings(config, save, changeView, width));

    private static void DrawSettings(Configuration config, Action save, Action<ViewMode> changeView, float width)
    {
        SettingsControls.ButtonVisibleRow(config, save, width);
        ImGui.Spacing();
        SettingsControls.ButtonStyleRow(config, save, width);
        ImGui.Spacing();
        SettingsControls.ViewRow(config, changeView, width);
        ImGui.Spacing();
        SettingsControls.HideInCombatRow(config, save, width);
    }
}
