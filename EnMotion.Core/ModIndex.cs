namespace EnMotion.Core;

/// <summary>Eine installierte Mod, gefuehrt ueber ihr Verzeichnis (eindeutig); der Name ist nur die Anzeige.</summary>
public sealed record ModRef(string Directory, string Name);

/// <summary>
/// Welche Mod welche Emotes aendert und in welcher Option, aus den Changed Items und den Dateien der Mods
/// gebaut. Der Aufbau kostet einen Penumbra-Aufruf und das Lesen der Dateien je Mod, deshalb wird der Index
/// nur bei Penumbra-Ereignissen neu gebaut und dann gelesen.
/// </summary>
public sealed class ModIndex
{
    private const string Prefix = "Emote: ";

    private readonly Dictionary<string, List<ModRef>> modsByEmote = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly Dictionary<string, List<string>> emotesByMod = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<VariantEntry>> variantsByEmote = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly Dictionary<Variant, List<string>> emotesByVariant = [];
    private readonly Dictionary<string, ModLayout> layouts = new(StringComparer.Ordinal);

    public static ModIndex Empty { get; } = new();

    /// <summary>Baut den Index; Eintraege ohne den Praefix "Emote: " (Gegenstaende, Aktionen) zaehlen nicht.</summary>
    public static ModIndex Build(IEnumerable<(ModRef Mod, IEnumerable<string> ChangedItems)> mods) =>
        Build(mods, null, null);

    /// <param name="mods">Mods mit ihren Changed Items.</param>
    /// <param name="layoutOf">Dateien einer Mod; null heisst unbekannt, dann gilt die Mod als Ganzes als Variante.</param>
    /// <param name="emotesOfKey">Emote-Namen zu einem ActionTimeline-Schluessel (klein).</param>
    public static ModIndex Build(
        IEnumerable<(ModRef Mod, IEnumerable<string> ChangedItems)> mods,
        Func<ModRef, ModLayout?>? layoutOf,
        Func<string, IEnumerable<string>>? emotesOfKey)
    {
        var index = new ModIndex();
        var order = new List<ModRef>();

        foreach (var (mod, items) in mods)
        {
            foreach (var item in items)
            {
                if (!item.StartsWith(Prefix, StringComparison.Ordinal))
                    continue;

                var emote = item[Prefix.Length..].Trim();
                if (emote.Length == 0)
                    continue;

                if (!index.modsByEmote.TryGetValue(emote, out var forEmote))
                    index.modsByEmote[emote] = forEmote = [];

                if (!forEmote.Contains(mod))
                    forEmote.Add(mod);

                if (!index.emotesByMod.TryGetValue(mod.Directory, out var forMod))
                    index.emotesByMod[mod.Directory] = forMod = [];

                if (!forMod.Contains(emote, StringComparer.CurrentCultureIgnoreCase))
                    forMod.Add(emote);

                if (!order.Contains(mod))
                    order.Add(mod);
            }
        }

        index.BuildVariants(order, layoutOf, emotesOfKey);
        return index;
    }

    private void BuildVariants(List<ModRef> mods, Func<ModRef, ModLayout?>? layoutOf, Func<string, IEnumerable<string>>? emotesOfKey)
    {
        // Je Mod: welche Emotes die Standarddateien und jede Option aendern (nur Emotes, die Penumbra fuer die Mod meldet).
        var maps = new Dictionary<string, (HashSet<string> Default, List<(OptionGroup Group, ModOption Option, HashSet<string> Emotes)> Options)>();

        foreach (var mod in mods)
        {
            var layout = layoutOf?.Invoke(mod);
            if (layout is null || emotesOfKey is null)
                continue;

            layouts[mod.Directory] = layout;
            var own = new HashSet<string>(emotesByMod[mod.Directory], StringComparer.CurrentCultureIgnoreCase);

            HashSet<string> Names(IEnumerable<string> paths)
            {
                var set = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                foreach (var path in paths)
                {
                    if (EmotePaths.KeyOf(path) is { } key)
                        set.UnionWith(emotesOfKey(key).Where(own.Contains));
                }

                return set;
            }

            var options = new List<(OptionGroup, ModOption, HashSet<string>)>();
            foreach (var group in layout.Groups)
                foreach (var option in group.Options)
                    options.Add((group, option, Names(option.Paths)));

            maps[mod.Directory] = (Names(layout.DefaultPaths), options);
        }

        foreach (var (emote, emoteMods) in modsByEmote)
        {
            var entries = new List<VariantEntry>();

            void Add(Variant variant, int groupPriority = 0, int optionPriority = 0)
            {
                entries.Add(new VariantEntry(variant, entries.Count, groupPriority, optionPriority));

                if (!emotesByVariant.ContainsKey(variant))
                    emotesByVariant[variant] = [];
            }

            foreach (var mod in emoteMods)
            {
                if (!maps.TryGetValue(mod.Directory, out var map))
                {
                    Add(new Variant(mod));
                    continue;
                }

                var found = map.Options.Where(o => o.Emotes.Contains(emote)).ToList();
                var inDefault = map.Default.Contains(emote);

                // Greift die Zuordnung nicht, bleibt die Mod als Ganzes waehlbar.
                if (found.Count == 0 && !inDefault)
                {
                    Add(new Variant(mod));
                    continue;
                }

                if (inDefault)
                    Add(new Variant(mod));

                foreach (var (group, option, _) in found)
                    Add(new Variant(mod, group.Name, option.Name), group.Priority, option.Priority);
            }

            variantsByEmote[emote] = entries;
        }

        // Weitere Emotes je Variante: bei Optionen und bei Standarddateien die, die sie aendern; sonst alle der Mod.
        foreach (var variant in emotesByVariant.Keys.ToList())
        {
            var all = emotesByMod[variant.Mod.Directory];
            var list = all;

            if (maps.TryGetValue(variant.Mod.Directory, out var map))
            {
                var set = variant.IsOption
                    ? map.Options.FirstOrDefault(o => o.Group.Name == variant.Group && o.Option.Name == variant.Option).Emotes
                    : map.Default;

                if (set is { Count: > 0 })
                    list = all.Where(set.Contains).ToList();
            }

            emotesByVariant[variant] = list;
        }
    }

    /// <summary>Alle Emote-Namen, die mindestens eine installierte Mod aendert.</summary>
    public IEnumerable<string> EmoteNames => modsByEmote.Keys;

    /// <summary>Die Mods, die das Emote aendern, in der Reihenfolge der Eingabe.</summary>
    public IReadOnlyList<ModRef> ModsOf(string emote) =>
        modsByEmote.TryGetValue(emote, out var list) ? list : [];

    /// <summary>Die Emotes, die die Mod aendert.</summary>
    public IReadOnlyList<string> EmotesOf(string directory) =>
        emotesByMod.TryGetValue(directory, out var list) ? list : [];

    /// <summary>Die waehlbaren Varianten eines Emotes, nach Mods geordnet.</summary>
    public IReadOnlyList<VariantEntry> VariantsOf(string emote) =>
        variantsByEmote.TryGetValue(emote, out var list) ? list : [];

    /// <summary>Die Emotes, die diese Variante aendert (einschliesslich des gewaehlten).</summary>
    public IReadOnlyList<string> EmotesOf(Variant variant) =>
        emotesByVariant.TryGetValue(variant, out var list) ? list : [];

    /// <summary>Die Optionen der Mod, die das Emote aendern (Gruppe, Option).</summary>
    public IEnumerable<(string Group, string Option)> OptionsOf(string directory, string emote) =>
        VariantsOf(emote)
            .Where(e => e.Variant.IsOption && e.Variant.Mod.Directory == directory)
            .Select(e => (e.Variant.Group!, e.Variant.Option!));

    /// <summary>Die gelesenen Dateien der Mod oder null.</summary>
    public ModLayout? LayoutOf(string directory) => layouts.GetValueOrDefault(directory);
}
