using System.Numerics;
using Dalamud.Bindings.ImGui;
using EnMotion.Core;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>
/// Die Plus-Kachel am Ende der Emote-Favoritenreihe: Über sie kommen ungemoddete Emotes als Favoriten dazu. Die
/// Reiter zeigen weiter nur Gemoddetes.
/// </summary>
public sealed partial class MainWindow
{
    private const string PlusPopup = "##favhinzu";

    private IReadOnlyList<EmoteEntry>? alleEmotes;
    private string plusSuche = string.Empty;
    private string plusListeSuche = string.Empty;
    private IReadOnlyList<ListBuilder.AddCandidate>? plusListe;
    private bool plusFokus;
    private Vector2 plusEcke;

    /// <summary>Die ganze Emote-Tabelle, einmal geladen, sobald ein ungemoddeter Favorit oder das Popup sie braucht.</summary>
    private IReadOnlyList<EmoteEntry> AlleEmotes => alleEmotes ??= EmoteCatalog.LoadAll(Plugin.Data);

    /// <summary>Plus-Kachel in Kachelgröße samt Popup. Nicht ziehbar und nicht Teil der sortierbaren Elemente.</summary>
    private void DrawPlusKachel()
    {
        var kante = config.IconSize;
        var ecke = ImGui.GetCursorScreenPos();
        var max = ecke + new Vector2(kante);

        if (ImGui.InvisibleButton("##plus", new Vector2(kante)))
        {
            plusSuche = string.Empty;
            plusListe = null;
            plusFokus = true;
            ImGui.OpenPopup(PlusPopup);
        }

        var ueber = ImGui.IsItemHovered();
        plusEcke = new Vector2(ecke.X, max.Y + Widgets.S(2f) + (config.ShowNames ? 2f + ImGui.GetTextLineHeight() : 0f));

        var liste = ImGui.GetWindowDrawList();

        if (ueber)
            liste.AddRectFilled(ecke, max, Theme.U32(Theme.Hover), 6f);

        liste.AddRect(ecke, max, Theme.U32(ueber ? Theme.Accent : Theme.Border), 6f, ImDrawFlags.None, 1f);

        var mitte = (ecke + max) / 2f;
        var halb = kante * 0.2f;
        var farbe = Theme.U32(ueber ? Theme.AccentLight : Theme.Muted);
        var dicke = Widgets.S(2f);

        liste.AddLine(mitte - new Vector2(halb, 0f), mitte + new Vector2(halb, 0f), farbe, dicke);
        liste.AddLine(mitte - new Vector2(0f, halb), mitte + new Vector2(0f, halb), farbe, dicke);

        if (ueber && !Zieht)
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(Loc.T("Add a favourite", "Favorit hinzufügen"));
            ImGui.TextColored(Theme.Muted, Loc.T("Any unlocked emote, also unmodded ones", "Jedes freigeschaltete Emote, auch ungemoddete"));
            ImGui.EndTooltip();
        }

        DrawPlusPopup();
    }

    private void DrawPlusPopup()
    {
        ImGui.SetNextWindowPos(plusEcke, ImGuiCond.Appearing);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Widgets.S(12f)));
        var offen = ImGui.BeginPopup(PlusPopup);
        ImGui.PopStyleVar();

        if (!offen)
            return;

        var breite = Widgets.S(300f);

        ImGui.TextColored(Theme.AccentLight, Loc.T("Add a favourite", "Favorit hinzufügen"));

        if (plusFokus)
        {
            ImGui.SetKeyboardFocusHere();
            plusFokus = false;
        }

        ImGui.SetNextItemWidth(breite);
        ImGui.InputTextWithHint("##plussuche", Suchhinweis, ref plusSuche, 64);

        if (plusListe is null || plusListeSuche != plusSuche)
        {
            plusListe = ListBuilder.AddCandidates(AlleEmotes, favourites, plusSuche, e => EmoteRunner.IsUnlocked(e.Id));
            plusListeSuche = plusSuche;
        }

        if (plusListe.Count == 0)
            ImGui.TextColored(Theme.Muted, Loc.T("No matches", "Keine Treffer"));
        else
            DrawPlusListe(plusListe, breite);

        ImGui.EndPopup();
    }

    private void DrawPlusListe(IReadOnlyList<ListBuilder.AddCandidate> kandidaten, float breite)
    {
        var zeile = Widgets.S(28f);
        var abstand = ImGui.GetStyle().ItemSpacing.Y;
        var hoehe = Math.Min(kandidaten.Count * (zeile + abstand), Widgets.S(320f));
        var scrollt = kandidaten.Count * (zeile + abstand) > hoehe;
        var schon = Loc.T("Favourite", "Favorit");
        var schonBreite = ImGui.CalcTextSize(schon).X;
        var iconKante = Widgets.S(24f);

        if (!ImGui.BeginChild("##plusliste", new Vector2(breite + (scrollt ? ImGui.GetStyle().ScrollbarSize : 0f), hoehe)))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var kandidat in kandidaten)
        {
            ImGui.PushID((int)kandidat.Emote.Id);

            var start = ImGui.GetCursorScreenPos();
            var gewaehlt = ImGui.Selectable("##eintrag", false,
                kandidat.Favourite ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None, new Vector2(breite, zeile));

            var liste = ImGui.GetWindowDrawList();
            var symbol = Plugin.Textures.GetFromGameIcon(new(kandidat.Emote.IconId)).GetWrapOrEmpty();
            var iconMin = start + new Vector2(Widgets.S(2f), (zeile - iconKante) / 2f);

            liste.AddImageRounded(symbol.Handle, iconMin, iconMin + new Vector2(iconKante), Vector2.Zero, Vector2.One,
                ImGui.GetColorU32(new Vector4(1f, 1f, 1f, kandidat.Favourite ? 0.5f : 1f)), 4f);

            var textX = start.X + iconKante + Widgets.S(10f);
            var textY = start.Y + (zeile - ImGui.GetTextLineHeight()) / 2f;
            var platz = breite - (textX - start.X) - (kandidat.Favourite ? schonBreite + Widgets.S(12f) : Widgets.S(6f));

            liste.AddText(new Vector2(textX, textY), Theme.U32(kandidat.Favourite ? Theme.Muted : Theme.Text),
                Kuerzen(kandidat.Emote.Name, platz));

            if (kandidat.Favourite)
                liste.AddText(new Vector2(start.X + breite - schonBreite - Widgets.S(6f), textY), Theme.U32(Theme.Accent), schon);

            ImGui.PopID();

            if (gewaehlt && favourites.Add(kandidat.Emote.Id))
            {
                save();
                ImGui.CloseCurrentPopup();
                break;
            }
        }

        ImGui.EndChild();
    }
}
