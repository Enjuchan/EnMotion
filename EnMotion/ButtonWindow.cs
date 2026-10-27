using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>
/// Ein rahmenloses Fenster mit einem einzigen Symbolknopf - sieht aus wie ein Feld
/// der Hotbar und oeffnet EnMotion.
///
/// Eingebaut statt ueber ein fremdes Plugin: Wer QoLBar nicht hat, soll trotzdem einen
/// Knopf haben.
/// </summary>
public sealed class ButtonWindow(Configuration config, Action onClick, Action save)
{
    private const float Kante = 48f;

    /// <summary>
    /// Ob die laufende Mausbewegung ein Ziehen ist. Ohne diese Unterscheidung oeffnet
    /// jedes Verschieben am Ende auch das Fenster - der Knopf meldet den Klick beim
    /// Loslassen, und da steht die Maus meist noch ueber ihm.
    /// </summary>
    private bool zieht;

    public void Draw()
    {
        if (!config.ButtonVisible)
            return;

        // NoMove immer: Das Fenster ist genau so gross wie der Knopf darin, es gibt
        // also keinen freien Hintergrund zum Ziehen. Verschoben wird stattdessen am
        // Knopf selbst, siehe unten.
        var flags = ImGuiWindowFlags.NoDecoration
                  | ImGuiWindowFlags.NoBackground
                  | ImGuiWindowFlags.AlwaysAutoResize
                  | ImGuiWindowFlags.NoScrollbar
                  | ImGuiWindowFlags.NoSavedSettings
                  | ImGuiWindowFlags.NoFocusOnAppearing
                  | ImGuiWindowFlags.NoMove;

        ImGui.SetNextWindowPos(new(config.ButtonX, config.ButtonY));

        // Ohne Innenabstand ist das Fenster exakt so gross wie der Knopf.
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        var offen = ImGui.Begin("##enmotionbutton", flags);
        ImGui.PopStyleVar();

        if (!offen)
        {
            ImGui.End();
            return;
        }

        var kante = Widgets.S(Kante);
        var pos = ImGui.GetCursorScreenPos();

        ImGui.PushID("enmotion");
        var geklickt = ImGui.InvisibleButton("##knopf", new Vector2(kante, kante));
        ImGui.PopID();

        var hover = ImGui.IsItemHovered();
        var draw = ImGui.GetWindowDrawList();
        var max = pos + new Vector2(kante, kante);
        var rand = Theme.U32(hover ? Theme.Accent : Theme.Border);

        if (config.ButtonStyle == ButtonStyle.Hotbar)
        {
            draw.AddRectFilled(pos, max, Theme.U32(Theme.Hover), 6f);
            if (config.ButtonIconId != 0)
            {
                var wrap = Plugin.Textures.GetFromGameIcon(new(config.ButtonIconId)).GetWrapOrEmpty();
                draw.AddImageRounded(wrap.Handle, pos + Vector2.One, max - Vector2.One, Vector2.Zero, Vector2.One, 0xFFFFFFFF, 5f);
            }
            draw.AddRect(pos, max, rand, 6f, ImDrawFlags.None, 1f);
        }
        else
        {
            draw.AddRectFilled(pos, max, Theme.U32(Theme.Card), 8f);
            draw.AddRect(pos, max, rand, 8f, ImDrawFlags.None, 1f);

            var figur = FontAwesomeIcon.Running.ToIconString();
            ImGui.PushFont(UiBuilder.IconFont);
            var groesse = ImGui.CalcTextSize(figur);
            draw.AddText(pos + (new Vector2(kante, kante) - groesse) / 2, Theme.U32(Theme.AccentLight), figur);
            ImGui.PopFont();
        }

        // Ziehen am Knopf selbst. Der Schwellwert verhindert, dass ein Klick mit
        // zittriger Hand schon als Verschieben gilt.
        if (!config.ButtonLocked &&
            ImGui.IsItemActive() &&
            ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4f))
        {
            var schritt = ImGui.GetIO().MouseDelta;

            config.ButtonX += schritt.X;
            config.ButtonY += schritt.Y;

            zieht = true;
        }

        // Ein Klick zaehlt nur, wenn nicht gezogen wurde: Der Knopf meldet ihn beim
        // Loslassen, und da steht die Maus nach dem Verschieben meist noch ueber ihm.
        if (geklickt)
        {
            if (!zieht)
                onClick();

            if (zieht)
                save();

            zieht = false;
        }

        // Losgelassen, ohne dass der Knopf ausloest - etwa weil die Maus beim
        // Verschieben davongelaufen ist.
        if (zieht && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            zieht = false;
            save();
        }

        if (hover)
        {
            ImGui.SetTooltip(config.ButtonLocked
                ? "EnMotion"
                : "EnMotion\nDrag to move, unlock in the settings");
        }

        ImGui.End();
    }
}
