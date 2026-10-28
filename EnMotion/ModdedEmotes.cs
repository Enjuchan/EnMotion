using System.Text.Json;
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
/// Welche Option einer Mod ein Emote aendert, sagt Penumbra nicht. Das steht in den Dateien der Mod
/// (default_mod.json, group_*.json); die Spielpfade der Animationen werden ueber die
/// ActionTimeline-Schluessel der Emote-Tabelle den Emotes zugeordnet.
/// </summary>
public static class ModdedEmotes
{
    // Die Tabelle aendert sich waehrend einer Sitzung nicht; Find laeuft nur beim Neuaufbau der Liste.
    private static IReadOnlyList<EmoteEntry>? allEntries;
    private static Dictionary<string, EmoteEntry>? catalogByName;
    private static Dictionary<string, List<string>>? namesByKey;

    // Gelesene Moddateien je Mod. Der Stempel aus Aenderungszeiten und Groessen sagt, ob neu gelesen werden muss.
    private static readonly Dictionary<string, (long Stamp, ModLayout? Layout)> layoutCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Welche Mod welches Emote aendert, Stand des letzten <see cref="Find"/>. Menue und Tooltips lesen nur
    /// hieraus; es gibt keinen Penumbra-Aufruf pro Klick.
    /// </summary>
    public static ModIndex Index { get; private set; } = ModIndex.Empty;

    public static IReadOnlyList<ModdedEmote> Find(IDataManager data)
    {
        var entries = allEntries ??= EmoteCatalog.LoadAll(data);

        var catalog = catalogByName ??= entries
            .GroupBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.CurrentCultureIgnoreCase);

        var byKey = namesByKey ??= BuildKeyMap(entries);
        var root = PenumbraBridge.GetModRoot();

        // Alle installierten Mods, auch abgeschaltete: Was ein Mod aendert, sagt Penumbra unabhaengig davon,
        // ob er eingeschaltet ist.
        Index = ModIndex.Build(
            PenumbraBridge.GetMods()
                .Select(kv => (new ModRef(kv.Key, kv.Value), (IEnumerable<string>)PenumbraBridge.GetChangedItems(kv.Key, kv.Value).Keys))
                .ToList(),
            mod => root is null ? null : ReadLayout(root, mod),
            key => byKey.TryGetValue(key, out var names) ? names : []);

        // Ob gerade eine Mod wirkt, sagt die Sammlung: Sie enthaelt nur, was eingeschaltet ist.
        const string prefix = "Emote: ";
        var active = PenumbraBridge.GetActiveChangedItems().Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => k[prefix.Length..].Trim())
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var result = new List<ModdedEmote>();

        foreach (var emoteName in Index.EmoteNames)
        {
            // Ohne Eintrag in der Emote-Tabelle fehlen Symbol und Textbefehl - dann
            // liesse sich das Emote weder anzeigen noch ausfuehren.
            if (catalog.TryGetValue(emoteName, out var emote))
            {
                var mods = Index.ModsOf(emoteName).Select(m => m.Name).Distinct().ToList();
                result.Add(new ModdedEmote(emote, mods, NoneActive: !active.Contains(emoteName)));
            }
        }

        return [.. result.OrderBy(m => m.Emote.Name, StringComparer.CurrentCulture)];
    }

    /// <summary>ActionTimeline-Schluessel (klein) zu den Namen der Emotes, die ihn nutzen.</summary>
    private static Dictionary<string, List<string>> BuildKeyMap(IReadOnlyList<EmoteEntry> entries)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            foreach (var key in entry.AnimationKeys)
            {
                if (!map.TryGetValue(key, out var names))
                    map[key] = names = [];

                if (!names.Contains(entry.Name))
                    names.Add(entry.Name);
            }
        }

        return map;
    }

    private static ModLayout? ReadLayout(string root, ModRef mod)
    {
        var folder = Path.Combine(root, mod.Directory);
        if (!Directory.Exists(folder))
            return null;

        // Penumbra 1.7 fuehrt Gruppen und Standarddateien in meta.json (FileVersion 4) und benennt die alten Dateien
        // in .bak um; aelter gespeicherte Mods haben noch default_mod.json und group_*.json. .bak wird nie gelesen.
        var metaFile = Path.Combine(folder, "meta.json");
        var defaultFile = Path.Combine(folder, "default_mod.json");
        var groupFiles = Directory.EnumerateFiles(folder, "group_*.json")
            .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToList();

        var stamp = 17L;
        foreach (var file in groupFiles.Prepend(defaultFile).Prepend(metaFile).Select(f => new FileInfo(f)).Where(f => f.Exists))
            stamp = HashCode.Combine(stamp, file.Name, file.LastWriteTimeUtc.Ticks, file.Length);

        if (layoutCache.TryGetValue(mod.Directory, out var cached) && cached.Stamp == stamp)
            return cached.Layout;

        ModLayout? layout = null;

        try
        {
            layout = File.Exists(metaFile) ? ModLayoutReader.ParseMeta(File.ReadAllText(metaFile)) : null;

            layout ??= ModLayoutReader.Parse(
                File.Exists(defaultFile) ? File.ReadAllText(defaultFile) : null,
                groupFiles.Select(f => File.ReadAllText(f)).ToList());
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            // Die Mod bleibt dann als Ganzes waehlbar; die Liste soll an einer kaputten Datei nicht scheitern.
            Plugin.Log.Warning($"Could not read the files of mod {mod.Directory}: {e.Message}");
        }

        layoutCache[mod.Directory] = (stamp, layout);
        return layout;
    }
}
