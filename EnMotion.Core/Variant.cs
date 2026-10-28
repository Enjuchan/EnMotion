using System.Text.RegularExpressions;

namespace EnMotion.Core;

/// <summary>
/// Eine waehlbare Variante eines Emotes: eine Option einer Mod (<paramref name="Group"/> und
/// <paramref name="Option"/> gesetzt) oder die Mod als Ganzes, wo die Aenderung in ihren Standarddateien liegt.
/// </summary>
public sealed record Variant(ModRef Mod, string? Group = null, string? Option = null)
{
    public bool IsOption => Option is not null;
}

/// <summary>Eine Variante mit den Prioritaeten, nach denen Penumbra Konflikte loest; <paramref name="Order"/> ist die Reihenfolge im Menue.</summary>
public sealed record VariantEntry(Variant Variant, int Order, int GroupPriority, int OptionPriority);

/// <summary>Ordnet Spielpfade von Animationsdateien den ActionTimeline-Schluesseln der Emote-Tabelle zu.</summary>
public static partial class EmotePaths
{
    // chara/human/c0101/animation/a0001/bt_common/emote/pose01_loop.pap -> emote/pose01_loop
    // Das Skelett (a0001) und der Waffenordner (bt_common, bt_2km_emp, ...) gehoeren nicht zum Schluessel.
    [GeneratedRegex(@"/animation/[^/]+/[^/]+/(?<key>.+)\.pap$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    /// <summary>Der Schluessel im Stil der ActionTimeline-Tabelle (klein) oder null, wenn der Pfad keine Animation ist.</summary>
    public static string? KeyOf(string gamePath)
    {
        var match = KeyPattern().Match(ModLayoutReader.Normalize(gamePath));
        return match.Success ? match.Groups["key"].Value : null;
    }
}
