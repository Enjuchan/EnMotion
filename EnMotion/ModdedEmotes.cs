using Dalamud.Plugin.Services;
using EnMotion.Core;

namespace EnMotion;

/// <summary>
/// Bringt Penumbras Wissen und die Spieldaten zusammen.
///
/// Penumbra fuehrt veraenderte Emotes als "Emote: Name" - mit demselben lokalisierten
/// Namen, den auch die Emote-Tabelle des Spiels liefert. Deshalb genuegt ein Abgleich
/// ueber den Namen.
///
/// Der urspruenglich geplante Weg - Animationspfade bilden und von Penumbra aufloesen
/// lassen - waere deutlich aufwendiger gewesen: Der vollstaendige Pfad haengt am
/// Charaktermodell, also an Rasse und Geschlecht. Diese Arbeit hat Penumbra bereits
/// getan.
/// </summary>
public static class ModdedEmotes
{
    private const string Prefix = "Emote: ";

    public static IReadOnlyList<ModdedEmote> Find(IDataManager data)
    {
        var catalog = EmoteCatalog.LoadAll(data)
            .GroupBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.CurrentCultureIgnoreCase);

        // Massgeblich ist die Sammlung: Sie enthaelt nur, was tatsaechlich
        // eingeschaltet ist. Wer stattdessen alle Mods durchgeht, zeigt auch
        // Emotes an, die gerade gar nicht wirken.
        var active = PenumbraBridge.GetActiveChangedItems();

        var names = new List<string>();

        foreach (var item in active.Keys)
        {
            if (!item.StartsWith(Prefix, StringComparison.Ordinal))
                continue;

            var emoteName = item[Prefix.Length..].Trim();
            if (emoteName.Length > 0)
                names.Add(emoteName);
        }

        // Welcher Mod dahintersteckt, verraet die Sammlung nicht. Das steht nur in den
        // Mod-Listen - deshalb dort nachschlagen, aber ausschliesslich fuer Emotes,
        // die laut Sammlung ohnehin aktiv sind.
        var byEmote = names.ToDictionary(
            n => n,
            _ => new List<string>(),
            StringComparer.CurrentCultureIgnoreCase);

        foreach (var (directory, modName) in PenumbraBridge.GetMods())
        {
            foreach (var item in PenumbraBridge.GetChangedItems(directory, modName).Keys)
            {
                if (!item.StartsWith(Prefix, StringComparison.Ordinal))
                    continue;

                var emoteName = item[Prefix.Length..].Trim();

                if (byEmote.TryGetValue(emoteName, out var list) && !list.Contains(modName))
                    list.Add(modName);
            }
        }

        var result = new List<ModdedEmote>();

        foreach (var (emoteName, mods) in byEmote)
        {
            // Ohne Eintrag in der Emote-Tabelle fehlen Symbol und Textbefehl - dann
            // liesse sich das Emote weder anzeigen noch ausfuehren.
            if (catalog.TryGetValue(emoteName, out var emote))
                result.Add(new ModdedEmote(emote, mods));
        }

        return [.. result.OrderBy(m => m.Emote.Name, StringComparer.CurrentCulture)];
    }
}
