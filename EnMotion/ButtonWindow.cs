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

    public void Draw()
    {
        if (!config.ButtonVisible)
            return;

        var flags = ImGuiWindowFlags.NoDecoration
                  | ImGuiWindowFlags.NoBackground
                  | ImGuiWindowFlags.AlwaysAutoResize
                  | ImGuiWindowFlags.NoScrollbar
                  | ImGuiWindowFlags.NoSavedSettings
                  | ImGuiWindowFlags.NoFocusOnAppearing;

        if (config.ButtonLocked)
            flags |= ImGuiWindowFlags.NoMove;

        ImGui.SetNextWindowPos(new(config.ButtonX, config.ButtonY), ImGuiCond.FirstUseEver);

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

        if (geklickt)
            onClick();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(config.ButtonLocked
                ? "EnMotion"
                : "EnMotion  —  drag to move");
        }

        // Verschobene Position merken, aber nur bei echter Aenderung: Sonst schreibt
        // jeder Frame die Konfiguration auf die Platte.
        var pos = ImGui.GetWindowPos();

        if (Math.Abs(pos.X - config.ButtonX) > 0.5f || Math.Abs(pos.Y - config.ButtonY) > 0.5f)
        {
            config.ButtonX = pos.X;
            config.ButtonY = pos.Y;
            save();
        }

        ImGui.End();
    }
}
