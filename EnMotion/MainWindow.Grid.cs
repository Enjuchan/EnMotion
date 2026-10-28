using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using EnMotion.Core;
using EnMotion.Ui;

namespace EnMotion;

public sealed partial class MainWindow
{
    /// <summary>Wann ein Emote zuletzt abgespielt wurde, fuer das Aufleuchten der Kachel.</summary>
    private readonly Dictionary<uint, double> zuletztGespielt = [];

    private const double Leuchtdauer = 0.5;

    /// <summary>Mod-Filter je Reiter. Bewusst nicht gespeichert, damit nach einem Neustart nichts unbemerkt fehlt.</summary>
    private readonly Dictionary<EmoteKind, HashSet<string>> modFilter = new()
    {
        [EmoteKind.Emote] = [],
        [EmoteKind.Dance] = [],
        [EmoteKind.Expression] = [],
    };

    private static string Titel(EmoteKind art) => art switch
    {
        EmoteKind.Dance => Loc.T("Modded dances", "Gemoddete Tänze"),
        EmoteKind.Expression => Loc.T("Modded expressions", "Gemoddete Mimik"),
        _ => Loc.T("Modded emotes", "Gemoddete Emotes"),
    };

    private static string KeinEintrag(EmoteKind art) => art switch
    {
        EmoteKind.Dance => Loc.T("No modded dances found", "Keine gemoddeten Tänze gefunden"),
        EmoteKind.Expression => Loc.T("No modded expressions found", "Keine gemoddete Mimik gefunden"),
        _ => Loc.T("No modded emotes found", "Keine gemoddeten Emotes gefunden"),
    };

    private static string Suchhinweis => Loc.T("search...", "Suche...");

    private static string NurEintraege => Loc.T(
        "Only entries changed by an enabled mod show up here.",
        "Hier erscheinen nur Einträge, die ein aktivierter Mod verändert.");

    private bool Ausblenden(ModdedEmote m) => config.HideLockedEmotes && !EmoteRunner.IsUnlocked(m.Emote.Id);

    /// <summary>Mod-Filter und Modnamen des Reiters; raeumt Filter auf, die kein Chip oder Knopf mehr aufheben kann.</summary>
    private (HashSet<string> Filter, IReadOnlyList<string> Namen) FilterStand(EmoteKind art)
    {
        var filter = modFilter[art];
        var namen = ListBuilder.ModNames(gemoddet, art);

        // Ein Mod, der verschwunden ist, darf keinen Filter zuruecklassen, den kein Chip mehr aufheben kann.
        if (namen.Count <= 1)
            filter.Clear();
        else
            filter.RemoveWhere(n => !namen.Contains(n));

        return (filter, namen);
    }

    private void DrawEmoteTab(EmoteKind art, bool kompakt = false)
    {
        if (!penumbraDa)
        {
            Widgets.Card("penumbra", _ =>
            {
                Widgets.Icon(FontAwesomeIcon.Times, Theme.Danger);
                ImGui.SameLine();
                ImGui.TextUnformatted(Loc.T("Penumbra is not loaded", "Penumbra ist nicht geladen"));

                if (!string.IsNullOrEmpty(PenumbraBridge.LastError))
                    ImGui.TextColored(Theme.Muted, PenumbraBridge.LastError);
            });
            return;
        }

        DrawFavoritenReihe(kompakt);

        var titel = Titel(art);
        var (filter, namen) = FilterStand(art);

        if (!gemoddet.Any(m => m.Emote.Kind == art))
        {
            if (!kompakt)
                Widgets.SectionHeader(titel);

            DrawZustand("empty", KeinEintrag(art), kompakt ? null : NurEintraege);
            return;
        }

        // In der Kompaktansicht stehen Suche und Filterknopf in der Leiste.
        if (!kompakt && namen.Count > 1)
        {
            var label = filter.Count > 0 ? $"{Loc.T("Mods", "Mods")} · {filter.Count}" : Loc.T("Mods", "Mods");
            var knopf = FilterKnopfBreite(label);

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - knopf - ImGui.GetStyle().ItemSpacing.X);
            ImGui.InputTextWithHint("##suche", Suchhinweis, ref suche, 64);
            ImGui.SameLine();
            DrawFilterKnopf(label, filter.Count > 0);
            DrawModPopup(art, namen, filter);

            if (filter.Count > 0)
            {
                var erste = true;

                foreach (var name in filter.Order(StringComparer.CurrentCultureIgnoreCase).ToList())
                {
                    Widgets.Flow(ref erste, Widgets.TagChipWidth(name, false));

                    if (Widgets.TagChip($"mod{name}", name, tooltip: Loc.T("Mod filter", "Mod-Filter")))
                        filter.Remove(name);
                }
            }

            ImGui.Spacing();
        }
        else if (!kompakt)
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##suche", Suchhinweis, ref suche, 64);
        }

        var rest = ListBuilder.Grid(gemoddet, favourites, art, suche, filter, Ausblenden);

        if (!kompakt)
        {
            Widgets.SectionHeader($"{titel} · {rest.Count}",
                $"{NurEintraege} {Loc.T("Favourites stand above.", "Favoriten stehen darüber.")}");
        }

        if (rest.Count == 0)
        {
            DrawZustand("nomatch", Loc.T("No matches", "Keine Treffer"),
                kompakt ? null : Loc.T(
                    "Nothing fits the search or the mod filter, or the rest is pinned above or hidden as locked.",
                    "Nichts passt zur Suche oder zum Mod-Filter, oder der Rest ist oben angeheftet oder als nicht freigeschaltet ausgeblendet."));
            return;
        }

        ImGui.BeginChild("liste", new(0, 0));
        Reihe(rest, "rest");
        ImGui.EndChild();
    }

    private const string ModPopup = "##modfilter";
    private string modSuche = string.Empty;
    private Vector2 filterKnopfEcke;

    private static float FilterKnopfBreite(string label)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var icon = ImGui.CalcTextSize(FontAwesomeIcon.Filter.ToIconString()).X;
        ImGui.PopFont();
        return label.Length == 0 ? icon + 22f : icon + 6f + ImGui.CalcTextSize(label).X + 22f;
    }

    /// <summary>
    /// Knopf im Stil von Widgets.Chip, mit Filter-Icon; an heisst, ein Filter ist aktiv. Ohne Beschriftung
    /// bleibt nur das Icon, wie in der Kompaktansicht.
    /// </summary>
    private void DrawFilterKnopf(string label, bool an)
    {
        var liste = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var groesse = new Vector2(FilterKnopfBreite(label), ImGui.GetFrameHeight());
        var radius = groesse.Y / 2;

        if (ImGui.InvisibleButton("##filterknopf", groesse))
        {
            modSuche = string.Empty;
            ImGui.OpenPopup(ModPopup);
        }

        var ueber = ImGui.IsItemHovered();
        filterKnopfEcke = new Vector2(pos.X, pos.Y + groesse.Y + Widgets.S(2f));

        if (an)
            liste.AddRectFilled(pos, pos + groesse, Theme.U32(ueber ? Theme.AccentLight : Theme.Accent), radius);
        else
        {
            if (ueber)
                liste.AddRectFilled(pos, pos + groesse, Theme.U32(Theme.Hover), radius);
            liste.AddRect(pos, pos + groesse, Theme.U32(Theme.Border), radius, ImDrawFlags.None, 1f);
        }

        var farbe = Theme.U32(an ? Theme.OnAccent : ueber ? Theme.Text : Theme.Muted);
        var icon = FontAwesomeIcon.Filter.ToIconString();

        ImGui.PushFont(UiBuilder.IconFont);
        var iconGroesse = ImGui.CalcTextSize(icon);
        liste.AddText(new Vector2(pos.X + 11f, pos.Y + (groesse.Y - iconGroesse.Y) / 2), farbe, icon);
        ImGui.PopFont();

        if (label.Length > 0)
        {
            liste.AddText(new Vector2(pos.X + 17f + iconGroesse.X, pos.Y + (groesse.Y - ImGui.GetTextLineHeight()) / 2),
                farbe, label);
        }

        if (ueber)
            ImGui.SetTooltip(an && label.Length == 0
                ? Loc.T("Filter by mod (active)", "Nach Mod filtern (aktiv)")
                : Loc.T("Filter by mod", "Nach Mod filtern"));
    }

    /// <summary>Popup mit einem Schalter je Mod dieses Reiters und der Zahl seiner Eintraege.</summary>
    private void DrawModPopup(EmoteKind art, IReadOnlyList<string> namen, HashSet<string> filter)
    {
        ImGui.SetNextWindowPos(filterKnopfEcke, ImGuiCond.Appearing);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Widgets.S(12f)));
        var offen = ImGui.BeginPopup(ModPopup);
        ImGui.PopStyleVar();

        if (!offen)
            return;

        var zaehler = gemoddet
            .Where(m => m.Emote.Kind == art)
            .SelectMany(m => m.Mods)
            .GroupBy(n => n)
            .ToDictionary(g => g.Key, g => g.Count());

        var sichtbar = namen
            .Where(n => string.IsNullOrWhiteSpace(modSuche) || n.Contains(modSuche, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var zahlBreite = zaehler.Count == 0 ? 0f : zaehler.Values.Max(z => ImGui.CalcTextSize(z.ToString()).X);
        var luft = Widgets.S(12f);
        var schalter = 30f;
        var namenBreite = Math.Min(
            namen.Max(n => ImGui.CalcTextSize(n).X),
            Math.Max(Widgets.S(360f) - zahlBreite - schalter - luft * 2f, Widgets.S(80f)));
        var popupTitel = Loc.T("Filter by mod", "Nach Mod filtern");
        var gesamt = Math.Max(namenBreite + luft + zahlBreite + luft + schalter, ImGui.CalcTextSize(popupTitel).X);

        ImGui.TextColored(Theme.AccentLight, popupTitel);

        if (namen.Count > 8)
        {
            ImGui.SetNextItemWidth(gesamt);
            ImGui.InputTextWithHint("##modsuche", Suchhinweis, ref modSuche, 64);
        }

        var zeile = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var hoehe = Math.Min(sichtbar.Count * zeile, Widgets.S(300f));
        var scrollt = sichtbar.Count * zeile > hoehe;

        if (ImGui.BeginChild("##modliste", new Vector2(gesamt + (scrollt ? ImGui.GetStyle().ScrollbarSize : 0f), hoehe)))
        {
            foreach (var name in sichtbar)
            {
                ImGui.PushID(name);

                var start = ImGui.GetCursorScreenPos();
                var an = filter.Contains(name);

                ImGui.AlignTextToFramePadding();
                var kurz = Kuerzen(name, namenBreite);
                ImGui.TextUnformatted(kurz);
                if (kurz != name && ImGui.IsItemHovered())
                    ImGui.SetTooltip(name);

                var zahl = zaehler.GetValueOrDefault(name).ToString();
                ImGui.SetCursorScreenPos(new Vector2(
                    start.X + gesamt - schalter - luft - ImGui.CalcTextSize(zahl).X, start.Y));
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Theme.Muted, zahl);

                ImGui.SetCursorScreenPos(new Vector2(start.X + gesamt - schalter,
                    start.Y + (ImGui.GetFrameHeight() - 16f) / 2));
                if (Widgets.Toggle("##an", ref an))
                {
                    if (an)
                        filter.Add(name);
                    else
                        filter.Remove(name);
                }

                ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + zeile));
                ImGui.PopID();
            }
        }

        ImGui.EndChild();

        if (filter.Count > 0)
        {
            ImGui.Separator();

            if (ImGui.Selectable(Loc.T("Clear filter", "Filter zurücksetzen")))
                filter.Clear();
        }

        ImGui.EndPopup();
    }

    /// <summary>Zustand als Karte mit einer Muted-Zeile; die Erklaerung steht im Tooltip.</summary>
    private static void DrawZustand(string id, string text, string? help) =>
        Widgets.Card(id, _ =>
        {
            ImGui.TextColored(Theme.Muted, text);

            if (help is null)
                return;

            ImGui.SameLine();
            Widgets.HelpIcon(help);
        });

    /// <summary>
    /// Die gemeinsame Reihe der Favoriten, oben in Emotes, Dances, Expressions und Poses. Fehlen
    /// Favoriten, entfaellt sie samt Ueberschrift.
    /// </summary>
    private void DrawFavoritenReihe(bool kompakt = false)
    {
        var pinned = ListBuilder.Pinned(gemoddet, favourites, Ausblenden);

        if (pinned.Count > 0 || HatPosenFavoriten)
        {
            if (!kompakt)
            {
                Widgets.SectionHeader(Loc.T("Favourites", "Favoriten"),
                    Loc.T("Add or remove favourites with a right-click on a tile. Poses are favourited with a right-click on their chip in the Poses tab.",
                        "Füge Favoriten per Rechtsklick auf eine Kachel hinzu oder entferne sie. Posen markierst du per Rechtsklick auf ihren Chip im Tab Posen als Favorit."));
            }

            Reihe(pinned, "pin");
            DrawFavouritePoseChips(pinned.Count > 0);
            ImGui.Spacing();
        }

        if (kompakt)
            DrawTrennlinie();
    }

    /// <summary>Feine Linie zwischen Favoritenreihe und Gitter der Kompaktansicht.</summary>
    private static void DrawTrennlinie()
    {
        var pos = ImGui.GetCursorScreenPos();
        var breite = ImGui.GetContentRegionAvail().X;

        ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(breite, 0f), Theme.U32(Theme.CardBorder), 1f);
        ImGui.Dummy(new Vector2(breite, 1f));
    }

    /// <summary>Kacheln, die am Fensterrand umbrechen.</summary>
    private void Reihe(IReadOnlyList<ModdedEmote> eintraege, string kuerzel)
    {
        var erste = true;

        // Das Menue kann Favoriten aendern, waehrend die Reihe gezeichnet wird.
        foreach (var m in eintraege.ToList())
        {
            Widgets.Flow(ref erste, config.IconSize);
            Kachel(m, kuerzel);
        }
    }

    private void Kachel(ModdedEmote m, string kuerzel)
    {
        var id = m.Emote.Id;
        var frei = EmoteRunner.IsUnlocked(id);
        var benutzbar = frei && EmoteRunner.CanPlay(id);
        var angeheftet = favourites.Contains(id);

        var kante = config.IconSize;
        var hoehe = kante + (config.ShowNames ? 2f + ImGui.GetTextLineHeight() : 0f);
        var ecke = ImGui.GetCursorScreenPos();
        var max = ecke + new Vector2(kante, kante);

        ImGui.PushID($"{kuerzel}{id}");
        var geklickt = ImGui.InvisibleButton("##kachel", new Vector2(kante, hoehe));
        var ueber = ImGui.IsItemHovered();
        DrawMenue(m, benutzbar, angeheftet);
        ImGui.PopID();

        if (geklickt && benutzbar)
            Abspielen(id);

        var liste = ImGui.GetWindowDrawList();
        var alpha = !frei ? 0.35f : !benutzbar ? 0.6f : 1f;

        liste.AddRectFilled(ecke, max, Theme.U32(Theme.Hover), 6f);

        var symbol = Plugin.Textures.GetFromGameIcon(new(m.Emote.IconId)).GetWrapOrEmpty();
        liste.AddImageRounded(symbol.Handle, ecke + Vector2.One, max - Vector2.One, Vector2.Zero, Vector2.One,
            ImGui.GetColorU32(new Vector4(1f, 1f, 1f, alpha)), 5f);

        liste.AddRect(ecke, max, Theme.U32(ueber ? Theme.Accent : Theme.Border), 6f, ImDrawFlags.None, 1f);

        if (zuletztGespielt.TryGetValue(id, out var gespielt))
        {
            var verbleibend = 1.0 - (ImGui.GetTime() - gespielt) / Leuchtdauer;
            if (verbleibend > 0)
                liste.AddRect(ecke, max, Theme.U32(Theme.Accent with { W = (float)verbleibend }), 6f, ImDrawFlags.None, 2f);
            else
                zuletztGespielt.Remove(id);
        }

        if (!frei)
            DrawSchloss(liste, max);

        if (angeheftet)
            DrawHerz(liste, ecke, max, kante);

        if (config.ShowNames)
            liste.AddText(new Vector2(ecke.X, max.Y + 2f), Theme.U32(Theme.Muted), Kuerzen(m.Emote.Name, kante));

        if (ueber)
            DrawTooltip(m, frei, benutzbar);
    }

    private void Abspielen(uint id)
    {
        EmoteRunner.Play(id);
        zuletztGespielt[id] = ImGui.GetTime();

        if (config.CloseAfterPlay)
            IsOpen = false;
    }

    private void Umschalten(uint id)
    {
        favourites.Toggle(id);
        save();
    }

    /// <summary>
    /// Rechtsklick-Menue einer Kachel. Gehoert zum zuletzt gezeichneten Element, also zum
    /// Kachelknopf.
    /// </summary>
    private void DrawMenue(ModdedEmote m, bool benutzbar, bool angeheftet)
    {
        if (!ImGui.BeginPopupContextItem("##menue"))
            return;

        var id = m.Emote.Id;

        ImGui.TextColored(Theme.AccentLight, m.Emote.Name);
        ImGui.Separator();

        if (ImGui.Selectable(Loc.T("Play", "Abspielen"), false, benutzbar ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled))
            Abspielen(id);

        if (ImGui.Selectable(angeheftet
            ? Loc.T("Remove from favourites", "Aus Favoriten entfernen")
            : Loc.T("Add to favourites", "Zu Favoriten hinzufügen")))
            Umschalten(id);

        if (ImGui.Selectable(Loc.T("Copy command", "Befehl kopieren")))
            ImGui.SetClipboardText(m.Emote.TextCommand);

        foreach (var mod in m.Mods.ToList())
        {
            if (ImGui.Selectable(Loc.T($"Only show {mod}", $"Nur {mod} zeigen")))
                NurMod(m.Emote.Kind, mod);
        }

        ImGui.EndPopup();
    }

    /// <summary>Wechselt in den Reiter des Eintrags und zeigt dort nur diesen Mod.</summary>
    private void NurMod(EmoteKind art, string mod)
    {
        modFilter[art] = [mod];
        tab = (int)art;
        config.LastTab = tab;
        save();
    }

    /// <summary>
    /// Das Favoriten-Herz oben rechts, nur gezeichnet und ohne Trefferflaeche. Es waechst mit der
    /// Icon-Groesse; die Kontur in Card grenzt es vom Icon ab.
    /// </summary>
    private static void DrawHerz(ImDrawListPtr liste, Vector2 ecke, Vector2 max, float kante)
    {
        // Zwei Kreise vom Radius r und ein Dreieck darunter: Breite 4r, Hoehe etwa 3,4r.
        var r = Widgets.S(Math.Clamp(kante * 0.3f, 12f, 26f)) / 4f;
        var rand = Widgets.S(3f);
        var mitte = new Vector2(max.X - rand - 2f * r, ecke.Y + rand + 1.7f * r);

        Herz(liste, mitte, r + Widgets.S(0.75f), Theme.U32(Theme.Card));
        Herz(liste, mitte, r, Theme.U32(Theme.Accent));
    }

    private static void Herz(ImDrawListPtr liste, Vector2 mitte, float r, uint farbe)
    {
        const float Wurzel = 0.7071f;

        // Die Form reicht senkrecht von -r bis 2,414r um die Kreismitte; ihre Mitte liegt bei 0,707r.
        var kreise = mitte.Y - Wurzel * r;

        liste.AddCircleFilled(new Vector2(mitte.X - r, kreise), r, farbe);
        liste.AddCircleFilled(new Vector2(mitte.X + r, kreise), r, farbe);
        liste.AddTriangleFilled(
            new Vector2(mitte.X - r * (1f + Wurzel), kreise + Wurzel * r),
            new Vector2(mitte.X + r * (1f + Wurzel), kreise + Wurzel * r),
            new Vector2(mitte.X, kreise + r * (1f + 2f * Wurzel)),
            farbe);
    }

    private static void DrawSchloss(ImDrawListPtr liste, Vector2 max)
    {
        var text = FontAwesomeIcon.Lock.ToIconString();

        ImGui.PushFont(UiBuilder.IconFont);
        var groesse = ImGui.CalcTextSize(text);
        ImGui.PopFont();

        var abstand = Widgets.S(2f);
        var flaeche = max - new Vector2(abstand);
        var min = flaeche - groesse - new Vector2(abstand * 2f);

        liste.AddRectFilled(min, flaeche, Theme.U32(Theme.Background with { W = 0.85f }), 4f);

        ImGui.PushFont(UiBuilder.IconFont);
        liste.AddText(min + new Vector2(abstand), Theme.U32(Theme.Muted), text);
        ImGui.PopFont();
    }

    private static void DrawTooltip(ModdedEmote m, bool frei, bool benutzbar)
    {
        var zustand = !frei ? Loc.T("Not unlocked.", "Nicht freigeschaltet.")
            : !benutzbar ? Loc.T("Can't be used right now.", "Gerade nicht benutzbar.")
            : Loc.T("Click to play, right-click for more", "Klick spielt ab, Rechtsklick zeigt mehr");

        ImGui.BeginTooltip();
        ImGui.TextUnformatted(m.Emote.Name);
        ImGui.TextColored(Theme.Muted, m.Emote.TextCommand);
        ImGui.TextColored(Theme.Muted, $"{Loc.T("Mods", "Mods")}: {string.Join(", ", m.Mods)}");
        ImGui.TextColored(Theme.Muted, zustand);
        ImGui.EndTooltip();
    }

    /// <summary>Kuerzt auf die Breite; drei Punkte statt des Ellipsenzeichens, das in der Schrift fehlen kann.</summary>
    private static string Kuerzen(string text, float breite)
    {
        if (ImGui.CalcTextSize(text).X <= breite)
            return text;

        while (text.Length > 0 && ImGui.CalcTextSize(text + "...").X > breite)
            text = text[..^1];

        return text + "...";
    }
}
