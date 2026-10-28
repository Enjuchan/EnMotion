using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using EnMotion.Ui;

namespace EnMotion;

public sealed partial class MainWindow
{
    // ------------------------------------------------------------------ Posen

    /// <summary>Die angehefteten Posen der aktuellen Haltung, die es in ihr noch gibt.</summary>
    private IReadOnlyList<byte> FavorisiertePosen(PoseState stand) =>
        poseFavourites.IndicesOf((byte)stand.Type).Where(i => i < stand.Count).ToList();

    private bool HatPosenFavoriten => PoseController.Read() is { } stand && FavorisiertePosen(stand).Count > 0;

    /// <summary>
    /// Chips der angehefteten Posen der aktuellen Haltung in einer eigenen Zeile unter den Kacheln, per
    /// Strg+Ziehen sortierbar. Ohne solche Posen entfaellt die Zeile.
    /// </summary>
    private void DrawFavouritePoseChips()
    {
        if (PoseController.Read() is not { } stand)
            return;

        var indizes = FavorisiertePosen(stand);

        if (indizes.Count == 0)
            return;

        var erste = true;
        const string reihe = "posen";

        ZugBeginn(reihe);

        for (var i = 0; i < indizes.Count; i++)
        {
            var index = indizes[i];
            var label = $"Pose {index + 1}";
            Widgets.Flow(ref erste, Widgets.ChipWidth(label));
            PoseChip("fav", stand.Type, index, label, index == stand.Current, reihe, i);
        }

        ZugEnde(reihe, (von, nach) =>
        {
            if (poseFavourites.Move((byte)stand.Type, indizes.Contains, von, nach))
                save();
        });
    }

    /// <summary>
    /// Pose als Chip mit Herz, wenn angeheftet, und Rechtsklick-Menue; der Klick springt zur Pose. Mit
    /// <paramref name="reihe"/> ist der Chip per Strg+Ziehen sortierbar.
    /// </summary>
    private void PoseChip(string kuerzel, EmoteController.PoseType typ, byte index, string label, bool aktiv,
        string? reihe = null, int position = 0)
    {
        ImGui.PushID($"{kuerzel}{(byte)typ}-{index}");

        var tooltip = Loc.T("Click to switch to this pose, right-click for more", "Klick wechselt zu dieser Pose, Rechtsklick zeigt mehr");
        var a = 1f;

        bool geklickt;
        Vector2 min, max;

        if (reihe is null)
        {
            geklickt = Widgets.Chip("pose", label, aktiv, tooltip);
            min = ImGui.GetItemRectMin();
            max = ImGui.GetItemRectMax();
        }
        else
        {
            // Wie Widgets.Chip, aber mit Versatz fürs Gleiten und Ziehen; der Knopf bleibt im Layout stehen.
            var natur = ImGui.GetCursorScreenPos();
            var groesse = new Vector2(Widgets.ChipWidth(label), ImGui.GetFrameHeight());

            geklickt = ImGui.InvisibleButton("##chippose", groesse);
            var ueber = ImGui.IsItemHovered();
            geklickt = ZugPruefen(reihe, position, index, geklickt);

            var gezogen = IstGezogen(reihe, index);
            a = gezogen ? ZugAlpha : 1f;
            min = natur + Versatz(reihe, index);
            max = min + groesse;
            ChipZeichnen(gezogen ? ImGui.GetForegroundDrawList() : ImGui.GetWindowDrawList(), min, groesse, label, aktiv, ueber, a);

            if (ueber && !Zieht)
                ImGui.SetTooltip(tooltip + "\n" + SortierHinweis);
        }

        if (geklickt)
            PoseController.Request(typ, index);

        DrawPosenMenue(typ, index);
        ImGui.PopID();

        if (poseFavourites.Contains((byte)typ, index))
        {
            var r = Widgets.S(2.5f);
            var liste = reihe is not null && IstGezogen(reihe, index) ? ImGui.GetForegroundDrawList() : ImGui.GetWindowDrawList();
            var mitte = new Vector2(max.X - r * 1.5f, min.Y + r * 1.2f);

            Herz(liste, mitte, r + Widgets.S(0.75f), Abdunkeln(Theme.Card, a));
            Herz(liste, mitte, r, Abdunkeln(Theme.Accent, a));
        }
    }

    /// <summary>Zeichnet einen Chip wie <see cref="Widgets.Chip"/>, an beliebiger Stelle und mit Deckkraft.</summary>
    private static void ChipZeichnen(ImDrawListPtr liste, Vector2 pos, Vector2 groesse, string label, bool an, bool ueber, float a)
    {
        var radius = groesse.Y / 2;

        if (an)
            liste.AddRectFilled(pos, pos + groesse, Abdunkeln(ueber ? Theme.AccentLight : Theme.Accent, a), radius);
        else
        {
            if (ueber)
                liste.AddRectFilled(pos, pos + groesse, Abdunkeln(Theme.Hover, a), radius);
            liste.AddRect(pos, pos + groesse, Abdunkeln(Theme.Border, a), radius, ImDrawFlags.None, 1f);
        }

        var textY = pos.Y + (groesse.Y - ImGui.GetTextLineHeight()) / 2;
        liste.AddText(new Vector2(pos.X + 11, textY),
            Abdunkeln(an ? Theme.OnAccent : ueber ? Theme.Text : Theme.Muted, a), label);
    }

    /// <summary>Rechtsklick-Menue einer Pose; gehoert zum zuletzt gezeichneten Element, dem Chip.</summary>
    private void DrawPosenMenue(EmoteController.PoseType typ, byte index)
    {
        if (!ImGui.BeginPopupContextItem("##posemenue"))
            return;

        ImGui.TextColored(Theme.AccentLight, $"{PoseController.Describe(typ)} · Pose {index + 1}");
        ImGui.Separator();

        if (ImGui.Selectable(poseFavourites.Contains((byte)typ, index)
                ? Loc.T("Remove from favourites", "Aus Favoriten entfernen")
                : Loc.T("Add to favourites", "Zu Favoriten hinzufügen")))
            UmschaltenPose(typ, index);

        ImGui.EndPopup();
    }

    private void UmschaltenPose(EmoteController.PoseType typ, byte index)
    {
        poseFavourites.Toggle((byte)typ, index);
        save();
    }

    private void DrawPosen(bool kompakt = false)
    {
        DrawFavoritenReihe(kompakt);

        var stand = PoseController.Read();

        if (stand is null)
        {
            Widgets.Card("nochar", _ => ImGui.TextColored(Theme.Muted, Loc.T("No character loaded.", "Kein Charakter geladen.")));
            return;
        }

        var jetzt = stand.Value;

        if (kompakt)
            ImGui.BeginChild("posen", new Vector2(0, 0));

        Widgets.Card("aktuell", breite =>
        {
            var start = ImGui.GetCursorScreenPos();
            var rechts = Loc.T($"Pose {jetzt.Current + 1} of {jetzt.Count}", $"Pose {jetzt.Current + 1} von {jetzt.Count}");

            ImGui.TextColored(Theme.AccentLight, PoseController.Describe(jetzt.Type));
            ImGui.SetCursorScreenPos(new Vector2(start.X + breite - ImGui.CalcTextSize(rechts).X, start.Y));
            ImGui.TextColored(Theme.Muted, rechts);

            var erste = true;

            for (byte i = 0; i < jetzt.Count; i++)
            {
                var label = $"{i + 1}";
                Widgets.Flow(ref erste, Widgets.ChipWidth(label), 12f);
                PoseChip("cur", jetzt.Type, i, label, i == jetzt.Current);
            }

            if (EmoteLaeuft())
                ImGui.TextColored(Theme.Muted, Loc.T("An emote is running - the pose follows once it ends.", "Ein Emote läuft - die Pose folgt, sobald es endet."));
        }, selected: true);

        if (kompakt)
        {
            ImGui.EndChild();
            return;
        }

        // Angezeigt werden alle Haltungen, waehlbar ist nur die aktuelle: Eine Pose
        // laesst sich nur innerhalb der Haltung wechseln, in der man steht.
        foreach (var typ in Enum.GetValues<EmoteController.PoseType>())
        {
            if (!Enum.IsDefined(typ) || typ == jetzt.Type)
                continue;

            var angeheftet = poseFavourites.IndicesOf((byte)typ);

            Widgets.Card($"stance{(byte)typ}", _ =>
            {
                ImGui.TextColored(Theme.Muted, PoseController.Describe(typ));
                ImGui.TextColored(Theme.Muted, Loc.T("Switch to this stance to choose", "Wechsle in diese Haltung, um zu wählen"));

                var erste = true;

                foreach (var index in angeheftet)
                {
                    var label = $"Pose {index + 1}";
                    Widgets.Flow(ref erste, Widgets.TagChipWidth(label, false), 12f);

                    if (Widgets.TagChip($"{(byte)typ}-{index}", label))
                        UmschaltenPose(typ, index);
                }
            });
        }
    }
}
