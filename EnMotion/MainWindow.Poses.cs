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
    /// Chips der angehefteten Posen der aktuellen Haltung, am Ende der Favoritenreihe. Nach Kacheln
    /// stehen sie in deren Zeile und werden an der Kachelmitte ausgerichtet.
    /// </summary>
    private void DrawFavouritePoseChips(bool nachKacheln)
    {
        if (PoseController.Read() is not { } stand)
            return;

        var kachelOben = nachKacheln ? ImGui.GetItemRectMin().Y : 0f;
        var erste = !nachKacheln;
        var ersterChip = true;

        foreach (var index in FavorisiertePosen(stand))
        {
            var label = $"Pose {index + 1}";
            Widgets.Flow(ref erste, Widgets.ChipWidth(label));

            // Ein Chip, der in der Kachelzeile bleibt, sitzt sonst am oberen Kachelrand.
            if (nachKacheln && ersterChip && MathF.Abs(ImGui.GetCursorScreenPos().Y - kachelOben) < 1f)
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.SetCursorScreenPos(pos with { Y = pos.Y + (config.IconSize - ImGui.GetFrameHeight()) / 2f });
            }

            ersterChip = false;
            PoseChip("fav", stand.Type, index, label, index == stand.Current);
        }
    }

    /// <summary>Pose als Chip mit Herz, wenn angeheftet, und Rechtsklick-Menue; der Klick springt zur Pose.</summary>
    private void PoseChip(string kuerzel, EmoteController.PoseType typ, byte index, string label, bool aktiv)
    {
        ImGui.PushID($"{kuerzel}{(byte)typ}-{index}");

        if (Widgets.Chip("pose", label, aktiv, Loc.T("Click to switch to this pose, right-click for more", "Klick wechselt zu dieser Pose, Rechtsklick zeigt mehr")))
            PoseController.Request(typ, index);

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        DrawPosenMenue(typ, index);
        ImGui.PopID();

        if (poseFavourites.Contains((byte)typ, index))
        {
            var r = Widgets.S(2.5f);
            var liste = ImGui.GetWindowDrawList();
            var mitte = new Vector2(max.X - r * 1.5f, min.Y + r * 1.2f);

            Herz(liste, mitte, r + Widgets.S(0.75f), Theme.U32(Theme.Card));
            Herz(liste, mitte, r, Theme.U32(Theme.Accent));
        }
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
