namespace EnMotion.Core;

/// <summary>Einstellung einer Mod in der Sammlung: eingeschaltet, Prioritaet und gewaehlte Optionen je Gruppe.</summary>
public sealed record ModState(bool Enabled, int Priority, IReadOnlyDictionary<string, IReadOnlyList<string>> Options);

/// <summary>Rechnet die Optionen einer Mod fuer gewaehlte Varianten und bestimmt die wirkende Variante.</summary>
public static class VariantSettings
{
    /// <summary>
    /// Wendet die Wahlen nacheinander auf die Optionen der Mod an. Je Wahl werden die Optionen mit Mehrfachauswahl
    /// abgewaehlt, die dasselbe Emote aendern, dann die gewaehlte Option eingeschaltet; in einer Gruppe mit
    /// Einzelauswahl wird sie die Auswahl der Gruppe. Eine Wahl der Mod als Ganzes schaltet nur ab. Gruppen mit
    /// Einzelauswahl, die das Emote ebenfalls aendern, bleiben, weil dort immer etwas gewaehlt sein muss.
    /// </summary>
    public static Dictionary<string, List<string>> Apply(
        ModLayout? layout,
        IReadOnlyDictionary<string, IReadOnlyList<string>> current,
        IEnumerable<(string Emote, Variant Variant)> choices,
        Func<string, IEnumerable<(string Group, string Option)>> optionsChangingEmote)
    {
        var result = current.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.Ordinal);

        foreach (var (emote, variant) in choices)
        {
            foreach (var (group, option) in optionsChangingEmote(emote))
            {
                if (layout is not null && layout.IsMulti(group) && result.TryGetValue(group, out var selected))
                    selected.Remove(option);
            }

            if (variant is not { IsOption: true, Group: { } chosenGroup, Option: { } chosen })
                continue;

            if (layout is not null && layout.IsMulti(chosenGroup))
            {
                if (!result.TryGetValue(chosenGroup, out var selected))
                    result[chosenGroup] = selected = [];

                if (!selected.Contains(chosen))
                    selected.Add(chosen);
            }
            else
            {
                result[chosenGroup] = [chosen];
            }
        }

        return result;
    }

    /// <summary>
    /// Die Variante, die wirkt: bei den eingeschalteten Mods die mit der hoechsten Prioritaet, die das Emote unter
    /// ihren Optionen tatsaechlich aendert; in der Mod die gewaehlte Option mit der hoechsten Gruppen- und dann
    /// Optionspriorität, sonst die Standarddateien. Bei Gleichstand gilt die zuerst genannte.
    /// </summary>
    public static Variant? Effective(IReadOnlyList<VariantEntry> variants, Func<ModRef, ModState?> stateOf)
    {
        Variant? best = null;
        var bestPriority = int.MinValue;

        foreach (var perMod in variants.GroupBy(v => v.Variant.Mod))
        {
            if (stateOf(perMod.Key) is not { Enabled: true } state)
                continue;

            var inMod = perMod
                .Where(v => v.Variant.IsOption
                    && state.Options.TryGetValue(v.Variant.Group!, out var selected)
                    && selected.Contains(v.Variant.Option!))
                .OrderByDescending(v => v.GroupPriority)
                .ThenByDescending(v => v.OptionPriority)
                .ThenBy(v => v.Order)
                .Select(v => v.Variant)
                .FirstOrDefault()
                ?? perMod.Where(v => !v.Variant.IsOption).Select(v => v.Variant).FirstOrDefault();

            if (inMod is not null && (best is null || state.Priority > bestPriority))
            {
                best = inMod;
                bestPriority = state.Priority;
            }
        }

        return best;
    }
}
