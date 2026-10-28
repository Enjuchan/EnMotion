using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using EnMotion.Core;
using EnMotion.Ui;

namespace EnMotion;

public sealed partial class MainWindow
{
    // ------------------------------------------------------------ Kompaktansicht

    private static (FontAwesomeIcon Icon, string Tooltip)[] KompaktTabs() =>
    [
        (FontAwesomeIcon.HandPaper, Loc.T("Emotes", "Emotes")),
        (FontAwesomeIcon.Music, Loc.T("Dances", "Tänze")),
        (FontAwesomeIcon.Smile, Loc.T("Expressions", "Mimik")),
        (FontAwesomeIcon.Child, Loc.T("Poses", "Posen")),
    ];

    private void DrawKompakt()
    {
        DrawLeiste();

        if (tab < TabPosen)
            DrawEmoteTab((EmoteKind)tab, true);
        else
            DrawPosen(true);
    }

    /// <summary>Tabs, Suche, Filterknopf und der Knopf zur Vollansicht in einer Zeile.</summary>
    private void DrawLeiste()
    {
        if (Widgets.IconTabs("ktabs", KompaktTabs(), ref tab))
        {
            config.LastTab = tab;
            save();
        }

        var abstand = ImGui.GetStyle().ItemSpacing.X;
        var art = (EmoteKind)tab;
        var emoteTab = tab < TabPosen && penumbraDa;
        var (filter, namen) = emoteTab ? FilterStand(art) : (null, []);
        var hatFilter = emoteTab && namen.Count > 1;
        var hatSuche = emoteTab && gemoddet.Any(m => m.Emote.Kind == art);

        var rechts = Widgets.IconButtonWidth(FontAwesomeIcon.Expand);
        if (hatFilter)
            rechts += abstand + FilterKnopfBreite(string.Empty);

        if (hatSuche)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - rechts - abstand);
            ImGui.InputTextWithHint("##suche", Suchhinweis, ref suche, 64);
        }
        else
            ImGui.SameLine(ImGui.GetContentRegionMax().X - rechts);

        if (hatFilter)
        {
            ImGui.SameLine();
            DrawFilterKnopf(string.Empty, filter!.Count > 0);
            DrawModPopup(art, namen, filter);
        }

        ImGui.SameLine();
        if (Widgets.IconButton("ansicht", FontAwesomeIcon.Expand, Loc.T("Full view", "Vollansicht")))
            WechselAnsicht(ViewMode.Full);
    }
}
