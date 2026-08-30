using System.Numerics;
using Dalamud.Bindings.ImGui;
using EnMotion.Core;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace EnMotion;

/// <summary>
/// Drei Reiter: gemoddete Emotes, gemoddete Taenze, alle Posen.
///
/// Die Bindings heissen `Dalamud.Bindings.ImGui`, nicht ImGuiNET - Beispiele aus dem
/// Netz passen meist nicht.
/// </summary>
public sealed class MainWindow(Configuration config, Favourites favourites, Action save)
{
    private const uint Graufarbe = 0xFF999999u;
    private const uint Warnfarbe = 0xFF3333FFu;
    private const uint Aktivfarbe = 0xFFCC7733u;

    public bool Visible;

    private string suche = string.Empty;

    /// <summary>
    /// Penumbra jeden Frame zu befragen waere verschwenderisch - die Liste aendert
    /// sich nur, wenn jemand Mods umschaltet.
    /// </summary>
    private IReadOnlyList<ModdedEmote> gemoddet = [];
    private DateTime naechsteAbfrage = DateTime.MinValue;

    public void Draw()
    {
        if (!Visible)
            return;

        ImGui.SetNextWindowSize(new(660, 540), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("EnMotion", ref Visible))
        {
            ImGui.End();
            return;
        }

        Aktualisieren();

        if (ImGui.BeginTabBar("reiter"))
        {
            if (ImGui.BeginTabItem("Emotes"))
            {
                DrawGitter(taenze: false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Dances"))
            {
                DrawGitter(taenze: true);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Poses"))
            {
                DrawPosen();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Settings"))
            {
                DrawEinstellungen();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    /// <summary>Alle zwei Sekunden neu bei Penumbra nachfragen.</summary>
    private void Aktualisieren()
    {
        if (DateTime.UtcNow < naechsteAbfrage)
            return;

        gemoddet = ModdedEmotes.Find(Plugin.Data);
        naechsteAbfrage = DateTime.UtcNow.AddSeconds(2);
    }

    // ----------------------------------------------------------- Emotes/Taenze

    private void DrawGitter(bool taenze)
    {
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("##suche", "search...", ref suche, 64);

        if (!PenumbraBridge.IsAvailable)
        {
            ImGui.Spacing();
            ImGui.TextColored(Warnfarbe, "Penumbra is not available.");
            ImGui.TextColored(Graufarbe, PenumbraBridge.LastError ?? string.Empty);
            return;
        }

        var (pinned, rest) = ListBuilder.Build(gemoddet, favourites, suche, taenze);

        if (pinned.Count == 0 && rest.Count == 0)
        {
            ImGui.Spacing();

            ImGui.TextColored(Graufarbe, taenze
                ? "No modded dances found."
                : "No modded emotes found.");

            if (string.IsNullOrWhiteSpace(suche))
                ImGui.TextColored(Graufarbe, "Only emotes changed by an enabled mod show up here.");

            return;
        }

        ImGui.Spacing();
        ImGui.BeginChild("liste", new(0, 0));

        if (pinned.Count > 0)
        {
            Reihe(pinned, "pin");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        Reihe(rest, "rest");

        ImGui.EndChild();
    }

    /// <summary>Kacheln, die am Fensterrand umbrechen.</summary>
    private void Reihe(IReadOnlyList<ModdedEmote> eintraege, string kuerzel)
    {
        var breite = ImGui.GetContentRegionAvail().X;
        var kachel = config.IconSize + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2;
        var proZeile = Math.Max(1, (int)(breite / kachel));

        for (var i = 0; i < eintraege.Count; i++)
        {
            if (i % proZeile != 0)
                ImGui.SameLine();

            Kachel(eintraege[i], kuerzel);
        }
    }

    private void Kachel(ModdedEmote m, string kuerzel)
    {
        var id = m.Emote.Id;
        var frei = EmoteRunner.IsUnlocked(id);
        var angeheftet = favourites.Contains(id);

        var symbol = Plugin.Textures.GetFromGameIcon(new(m.Emote.IconId)).GetWrapOrDefault();

        if (!frei)
            ImGui.BeginDisabled();

        // ImageButton nimmt in diesen Bindings keine String-Kennung entgegen - die
        // Eindeutigkeit muss deshalb ueber PushID kommen. Ohne das halten zwei
        // Kacheln mit gleichem Symbol sich fuer denselben Knopf.
        ImGui.PushID($"{kuerzel}{id}");

        var geklickt = symbol is null
            ? ImGui.Button(m.Emote.Name, new(config.IconSize, config.IconSize))
            : ImGui.ImageButton(symbol.Handle, new(config.IconSize, config.IconSize));

        ImGui.PopID();

        if (!frei)
            ImGui.EndDisabled();

        if (geklickt && frei)
            EmoteRunner.Play(id);

        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        ImGui.SetTooltip(
            $"{m.Emote.Name}   {m.Emote.TextCommand}\n" +
            $"Mods: {string.Join(", ", m.Mods)}\n" +
            (frei
                ? angeheftet ? "Right-click to unpin" : "Right-click to pin"
                : "Not unlocked"));

        // Rechtsklick heftet an. Ein eigener Stern-Knopf waere bei dieser Kachelgroesse
        // nicht mehr zu treffen.
        if (!ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            return;

        favourites.Toggle(id);
        save();
    }

    // ------------------------------------------------------------------ Posen

    private void DrawPosen()
    {
        var stand = PoseController.Read();

        if (stand is null)
        {
            ImGui.TextColored(Graufarbe, "No character loaded.");
            return;
        }

        var jetzt = stand.Value;

        ImGui.TextUnformatted($"Current stance: {PoseController.Describe(jetzt.Type)}");

        if (PoseController.IsEmoting())
            ImGui.TextColored(Graufarbe, "An emote is running - the pose follows once it ends.");

        ImGui.Separator();
        ImGui.Spacing();

        // Angezeigt werden alle Haltungen, waehlbar ist nur die aktuelle: Eine Pose
        // laesst sich nur innerhalb der Haltung wechseln, in der man steht.
        foreach (var typ in Enum.GetValues<EmoteController.PoseType>())
        {
            if (!Enum.IsDefined(typ))
                continue;

            var aktuelle = typ == jetzt.Type;

            if (aktuelle)
                ImGui.TextUnformatted(PoseController.Describe(typ));
            else
                ImGui.TextColored(Graufarbe, PoseController.Describe(typ));

            if (!aktuelle)
            {
                ImGui.SameLine();
                ImGui.TextColored(Graufarbe, "  (switch to this stance to choose)");
                continue;
            }

            for (byte i = 0; i < jetzt.Count; i++)
            {
                ImGui.SameLine();

                var aktiv = i == jetzt.Current;

                if (aktiv)
                    ImGui.PushStyleColor(ImGuiCol.Button, Aktivfarbe);

                if (ImGui.Button($"{i + 1}##{typ}{i}", new(34, 0)))
                    PoseController.Request(typ, i);

                if (aktiv)
                    ImGui.PopStyleColor();
            }
        }
    }

    // ------------------------------------------------------------ Einstellungen

    private void DrawEinstellungen()
    {
        ImGui.Spacing();

        var sichtbar = config.ButtonVisible;
        if (ImGui.Checkbox("Show the button", ref sichtbar))
        {
            config.ButtonVisible = sichtbar;
            save();
        }

        ImGui.TextColored(Graufarbe, "The small icon that opens this window.");

        var gesperrt = config.ButtonLocked;
        if (ImGui.Checkbox("Lock its position", ref gesperrt))
        {
            config.ButtonLocked = gesperrt;
            save();
        }

        ImGui.TextColored(Graufarbe, "So it cannot be dragged away by accident.");

        ImGui.Separator();

        var groesse = config.IconSize;
        ImGui.SetNextItemWidth(220);
        if (ImGui.SliderFloat("Icon size", ref groesse, 24f, 96f, "%.0f"))
        {
            config.IconSize = groesse;
            save();
        }

        ImGui.Separator();
        ImGui.TextColored(Graufarbe, "Also reachable with /enmotion.");
    }
}
