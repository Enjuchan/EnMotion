using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using EnMotion.Core;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>
/// Sieben Reiter: gemoddete Emotes, Taenze und Ausdruecke, alle Posen, Einstellungen, Dependencies und About.
///
/// Die Bindings heissen `Dalamud.Bindings.ImGui`, nicht ImGuiNET - Beispiele aus dem
/// Netz passen meist nicht.
/// </summary>
public sealed partial class MainWindow(Configuration config, Favourites favourites, PoseFavourites poseFavourites, Action save, string version,
                  Dependencies dependencies, AboutInfo about, IPluginLog log)
{
    public bool Visible;

    private string suche = string.Empty;

    private static readonly TimeSpan Entprellung = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Rueckfall = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Die Abfrage kostet Millisekunden im Zeichenpfad. Deshalb laeuft sie nur nach
    /// Penumbra-Ereignissen, beim ersten Zeichnen und als Rueckfall alle 30 Sekunden.
    /// </summary>
    private IReadOnlyList<ModdedEmote> gemoddet = [];
    private DateTime naechsteAbfrage = DateTime.MinValue;
    private bool penumbraWarDa;

    private const int TabAnzahl = 7;
    private const int TabPosen = 3;
    private const int TabEinstellungen = 4;
    private const int TabDependencies = 5;

    // Settings, Dependencies und About gibt es nur in der Vollansicht.
    private int tab = Math.Clamp(config.LastTab, 0, config.View == ViewMode.Compact ? TabPosen : TabAnzahl - 1);

    /// <summary>Setzt die gemerkte Groesse der Ansicht einmalig, im Frame nach dem Umschalten.</summary>
    private bool groesseSetzen;

    private bool penumbraDa;
    private DateTime penumbraGeprueft = DateTime.MinValue;

    private readonly FrameTiming zeichenzeit = new(log, "Window draw time", TimeSpan.FromSeconds(5), () => config.ShowDiagnostics);
    private readonly FrameTiming abfragezeit = new(log, "Modded emote lookup", TimeSpan.FromSeconds(30), () => config.ShowDiagnostics);

    /// <summary>Groesse des Fensters im letzten gezeichneten Frame, fuer den Wechsel der Ansicht von aussen.</summary>
    private Vector2 letzteGroesse;

    private bool einstellungenAnfrage;

    /// <summary>Einstieg des Zahnrads im Plugin-Installer: Vollansicht mit dem Reiter Settings, ein zweiter Klick schliesst.</summary>
    public void ToggleSettings()
    {
        if (Visible && config.View == ViewMode.Full && tab == TabEinstellungen)
        {
            Visible = false;
            return;
        }

        Visible = true;
        einstellungenAnfrage = true;
    }

    public void Draw()
    {
        if (!Visible)
            return;

        var start = Stopwatch.GetTimestamp();

        using var theme = Theme.Push();

        // Nach dem Theme gesetzt, damit nur Hintergrund und Titelleiste die Deckkraft bekommen.
        var deckkraft = Math.Clamp(config.BackgroundOpacity, 0.2f, 1f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Theme.Background with { W = deckkraft });
        ImGui.PushStyleColor(ImGuiCol.TitleBg, Theme.Header with { W = deckkraft });
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Theme.Header with { W = deckkraft });
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, Theme.Header with { W = deckkraft });
        using var deckend = new Theme.Scope(4, 0);

        if (einstellungenAnfrage)
        {
            einstellungenAnfrage = false;

            if (config.View == ViewMode.Compact)
            {
                if (letzteGroesse.X > 0)
                    (config.CompactWidth, config.CompactHeight) = (letzteGroesse.X, letzteGroesse.Y);

                config.View = ViewMode.Full;
                groesseSetzen = true;
            }

            tab = TabEinstellungen;
            config.LastTab = tab;
            save();
        }

        var kompakt = config.View == ViewMode.Compact;

        if (groesseSetzen)
        {
            groesseSetzen = false;
            ImGui.SetNextWindowSize(GespeicherteGroesse(kompakt), ImGuiCond.Always);
        }
        else
            ImGui.SetNextWindowSize(StandardGroesse(kompakt), ImGuiCond.FirstUseEver);

        ImGui.SetNextWindowSizeConstraints(kompakt ? new Vector2(Widgets.S(240), Widgets.S(140)) : new Vector2(520, 400),
            new Vector2(float.MaxValue, float.MaxValue));

        var flags = config.WindowLocked ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize : ImGuiWindowFlags.None;

        // Ohne Titelleiste zieht ImGui das Fenster am freien Hintergrund.
        if (kompakt)
            flags |= ImGuiWindowFlags.NoTitleBar;

        // Der Titel bleibt "EnMotion", damit gespeicherte Fensterlage und -groesse erhalten bleiben.
        if (!ImGui.Begin("EnMotion", ref Visible, flags))
        {
            ImGui.End();
            return;
        }

        letzteGroesse = ImGui.GetWindowSize();

        Aktualisieren();

        Decor.Background(deckkraft);

        if (kompakt)
            DrawKompakt();
        else
            DrawVoll(deckkraft);

        ImGui.End();

        zeichenzeit.Add(Stopwatch.GetElapsedTime(start));
    }

    private static Vector2 StandardGroesse(bool kompakt) => kompakt ? new Vector2(Widgets.S(380), Widgets.S(280)) : new(720, 560);

    private Vector2 GespeicherteGroesse(bool kompakt)
    {
        var (breite, hoehe) = kompakt ? (config.CompactWidth, config.CompactHeight) : (config.FullWidth, config.FullHeight);
        return breite > 0 && hoehe > 0 ? new Vector2(breite, hoehe) : StandardGroesse(kompakt);
    }

    /// <summary>
    /// Wechselt die Ansicht. Die Lage bleibt, weil das Fenster seinen Namen behaelt; die Groesse der
    /// verlassenen Ansicht wird gemerkt und die der neuen im naechsten Frame gesetzt. Gehoert in den Zeichenpfad
    /// des Fensters, weil die aktuelle Groesse nur dort ablesbar ist.
    /// </summary>
    private void WechselAnsicht(ViewMode neu)
    {
        if (config.View == neu)
            return;

        var groesse = ImGui.GetWindowSize();

        if (config.View == ViewMode.Compact)
            (config.CompactWidth, config.CompactHeight) = (groesse.X, groesse.Y);
        else
            (config.FullWidth, config.FullHeight) = (groesse.X, groesse.Y);

        config.View = neu;
        groesseSetzen = true;

        if (neu == ViewMode.Compact && tab > TabPosen)
        {
            tab = 0;
            config.LastTab = 0;
        }

        save();
    }

    private void DrawVoll(float deckkraft)
    {
        Decor.HeaderBand(ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetTextLineHeight(), deckkraft);
        DrawKopf();
        ImGui.Dummy(new Vector2(0, 6));

        var tabs = new (FontAwesomeIcon Icon, string Label)[]
        {
            (FontAwesomeIcon.HandPaper, Beschriftung("Emote", "Emotes", EmoteKind.Emote)),
            (FontAwesomeIcon.Music, Beschriftung("Dance", "Dances", EmoteKind.Dance)),
            (FontAwesomeIcon.Smile, Beschriftung("Expression", "Expressions", EmoteKind.Expression)),
            (FontAwesomeIcon.Child, "Poses"),
            (FontAwesomeIcon.Cog, "Settings"),
            (FontAwesomeIcon.PuzzlePiece, "Dependencies"),
            (FontAwesomeIcon.InfoCircle, "About"),
        };

        if (Widgets.PillTabs("tabs", tabs, ref tab))
        {
            config.LastTab = tab;
            save();
        }

        ImGui.Dummy(new Vector2(0, 8));

        if (tab < TabPosen)
            DrawEmoteTab((EmoteKind)tab);
        else if (tab == TabPosen)
            DrawPosen();
        else if (tab == TabEinstellungen)
            DrawEinstellungen();
        else if (tab == TabDependencies)
        {
            ImGui.BeginChild("dependencies", new Vector2(0, 0));
            DependenciesView.Draw(dependencies);
            ImGui.EndChild();
        }
        else
        {
            ImGui.BeginChild("about", new Vector2(0, 0));
            AboutView.Draw(about);
            ImGui.EndChild();
        }
    }

    private void DrawKopf()
    {
        ImGui.AlignTextToFramePadding();
        Widgets.Icon(FontAwesomeIcon.Running, Theme.AccentLight);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(Theme.AccentLight, "EnMotion");
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(Theme.Muted, version);

        DrawStatusMarken();
        DrawStatuszeile();
    }

    /// <summary>
    /// Fragt Penumbra hoechstens alle zwei Sekunden, weil jede Pruefung ein IPC-Aufruf ist.
    /// So faellt auch auf, wenn Penumbra zur Laufzeit ausgeschaltet wird.
    /// </summary>
    private bool PenumbraDa()
    {
        var jetzt = DateTime.UtcNow;
        if (jetzt - penumbraGeprueft > TimeSpan.FromSeconds(2))
        {
            penumbraDa = PenumbraBridge.IsAvailable;
            penumbraGeprueft = jetzt;
        }

        return penumbraDa;
    }

    private void DrawStatusMarken()
    {
        var marken = new List<(string Text, Vector4 Punkt, string Tooltip)>();

        if (EmoteLaeuft())
            marken.Add(("Emote running", Theme.Accent, "An emote is playing right now."));

        var da = PenumbraDa();
        marken.Add(("Penumbra", da ? Theme.Success : Theme.Danger,
            da ? "Penumbra is loaded." : $"Penumbra can't be reached: {PenumbraBridge.LastError}"));

        var abstand = ImGui.GetStyle().ItemSpacing.X;
        var breite = marken.Sum(m => Widgets.PillWidth(m.Text, true)) + abstand * (marken.Count - 1)
            + abstand + Widgets.IconButtonWidth(FontAwesomeIcon.Compress);

        ImGui.SameLine(ImGui.GetContentRegionMax().X - breite);
        for (var i = 0; i < marken.Count; i++)
        {
            if (i > 0)
                ImGui.SameLine();

            Widgets.StatusPill(marken[i].Text, marken[i].Punkt);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(marken[i].Tooltip);
        }

        ImGui.SameLine();
        if (Widgets.IconButton("ansicht", FontAwesomeIcon.Compress, "Compact view"))
            WechselAnsicht(ViewMode.Compact);
    }

    private void DrawStatuszeile()
    {
        string text;

        if (!penumbraDa)
            text = "Penumbra is required.";
        else
        {
            var mods = gemoddet.SelectMany(m => m.Mods).Distinct().Count();

            var emotes = Anzahl(EmoteKind.Emote);
            var tanz = Anzahl(EmoteKind.Dance);
            var ausdruecke = Anzahl(EmoteKind.Expression);

            text = $"{emotes} modded {(emotes == 1 ? "emote" : "emotes")} · {tanz} {(tanz == 1 ? "dance" : "dances")} · "
                + $"{ausdruecke} {(ausdruecke == 1 ? "expression" : "expressions")} · {mods} {(mods == 1 ? "mod" : "mods")}";
        }

        var start = ImGui.GetCursorPosX();
        ImGui.TextColored(Theme.Muted, text);

        if (!config.ShowDiagnostics)
            return;

        // Rechtsbuendig, aber nie ueber den Statustext hinweg.
        var zeit = zeichenzeit.Summary();
        ImGui.SameLine(Math.Max(start + ImGui.CalcTextSize(text).X + ImGui.GetStyle().ItemSpacing.X * 2,
            ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(zeit).X));
        ImGui.TextColored(Theme.Muted, zeit);
    }

    private string Beschriftung(string einzahl, string mehrzahl, EmoteKind art)
    {
        var n = Anzahl(art);
        return $"{(n == 1 ? einzahl : mehrzahl)} · {n}";
    }

    private int Anzahl(EmoteKind art) => gemoddet.Count(m => m.Emote.Kind == art);

    /// <summary>
    /// EmoteController.IsEmoting meldete im Spiel dauerhaft wahr. Verlaesslich ist die
    /// Haltung: Waehrend eines Emotes ist sie nicht ablesbar, und daran erkennt auch
    /// PoseController ein laufendes Emote.
    /// </summary>
    private static bool EmoteLaeuft() =>
        PoseController.Read() is not null && !PoseController.HasValidPose();

    private void Aktualisieren()
    {
        if (!PenumbraDa())
        {
            penumbraWarDa = false;
            if (gemoddet.Count > 0)
                gemoddet = [];

            return;
        }

        // Erstes Zeichnen und Wiederkehr von Penumbra fragen sofort ab.
        var sofort = !penumbraWarDa;
        penumbraWarDa = true;

        if (!sofort && DateTime.UtcNow < naechsteAbfrage && !PenumbraBridge.TryTakeChange(Entprellung))
            return;

        // Vor der Abfrage, damit ein Ereignis waehrend der Abfrage nicht verloren geht.
        PenumbraBridge.ClearChange();

        var start = Stopwatch.GetTimestamp();
        gemoddet = ModdedEmotes.Find(Plugin.Data);
        abfragezeit.Add(Stopwatch.GetElapsedTime(start));

        naechsteAbfrage = DateTime.UtcNow + Rueckfall;
    }
}
