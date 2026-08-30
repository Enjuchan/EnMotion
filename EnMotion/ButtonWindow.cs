using System.Numerics;
using Dalamud.Bindings.ImGui;

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

        if (!ImGui.Begin("##enmotionbutton", flags))
        {
            ImGui.End();
            return;
        }

        var symbol = Plugin.Textures.GetFromGameIcon(new(config.ButtonIconId)).GetWrapOrDefault();

        // ImageButton nimmt in diesen Bindings keine String-Kennung entgegen.
        ImGui.PushID("enmotion");

        var geklickt = symbol is null
            ? ImGui.Button("EnMotion", new(Kante, Kante))
            : ImGui.ImageButton(symbol.Handle, new(Kante, Kante));

        ImGui.PopID();

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

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(config.ButtonLocked
                ? "EnMotion"
                : "EnMotion\nDrag to move, unlock in the settings");
        }

        ImGui.End();
    }
}
