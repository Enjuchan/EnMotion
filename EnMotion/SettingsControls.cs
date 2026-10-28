using System.Numerics;
using Dalamud.Bindings.ImGui;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>
/// Die Bedienelemente für Knopf zeigen, Knopfstil, Ansicht und Ausblenden im Kampf, die der Settings-Tab und das
/// Willkommensfenster teilen, damit beide dieselben Felder schreiben und sofort speichern.
/// </summary>
public static class SettingsControls
{
    /// <summary>Beschriftung links, Umschaltleiste rechtsbündig; <paramref name="changeView"/> wechselt die Ansicht samt Fenstergröße.</summary>
    public static void ViewRow(Configuration config, Action<ViewMode> changeView, float width)
    {
        var start = ImGui.GetCursorScreenPos();
        var view = config.View;

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(Loc.T("View", "Ansicht"));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T("Full for browsing and settings, Compact for leaving the window open while playing.",
                "Voll zum Stöbern und für die Einstellungen, Kompakt, um das Fenster beim Spielen offen zu lassen."));
        }

        var field = Widgets.S(180);
        ImGui.SetCursorScreenPos(new Vector2(start.X + width - field, start.Y));
        if (Widgets.Segmented("view", [ViewMode.Full, ViewMode.Compact],
                m => m == ViewMode.Full ? Loc.T("Full", "Voll") : Loc.T("Compact", "Kompakt"), ref view, field))
            changeView(view);
    }

    public static void HideInCombatRow(Configuration config, Action save, float width)
    {
        var combat = config.HideInCombat;
        if (Widgets.ToggleRow(Loc.T("Hide in combat", "Im Kampf ausblenden"), ref combat, width,
                Loc.T("The window and the button are not drawn during combat and come back afterwards.",
                    "Fenster und Knopf werden im Kampf nicht gezeichnet und kommen danach von selbst wieder.")))
        {
            config.HideInCombat = combat;
            save();
        }
    }

    public static void ButtonVisibleRow(Configuration config, Action save, float width)
    {
        var visible = config.ButtonVisible;
        if (Widgets.ToggleRow(Loc.T("Show button", "Knopf zeigen"), ref visible, width))
        {
            config.ButtonVisible = visible;
            save();
        }
    }

    public static void ButtonStyleRow(Configuration config, Action save, float width)
    {
        var start = ImGui.GetCursorScreenPos();
        var style = config.ButtonStyle;
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(Loc.T("Style", "Stil"));
        var field = Widgets.S(180);
        ImGui.SetCursorScreenPos(new Vector2(start.X + width - field, start.Y));
        if (Widgets.Segmented("buttonstyle", [ButtonStyle.Hotbar, ButtonStyle.EnMotion], m => m.ToString(), ref style, field))
        {
            config.ButtonStyle = style;
            save();
        }
    }
}
