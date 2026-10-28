namespace EnMotion.Core;

/// <summary>Die Wahl eines Emotes.</summary>
public sealed record Choice(uint EmoteId, string EmoteName, Variant Variant);

/// <summary>
/// Merkt, welche Variante EnMotion je Emote gesetzt hat. Penumbra speichert vorlaeufige Einstellungen je Mod,
/// nicht je Emote: Die Wahlen aller Emotes einer Mod werden zusammengefuehrt und die Mod mit dem Ergebnis neu
/// gesetzt; eine Mod ohne Wahl wird entfernt. Die Rueckgaben nennen die Mods, die danach neu zu setzen sind.
/// </summary>
public sealed class VariantBook
{
    private readonly List<Choice> choices = [];

    public Variant? ChosenFor(uint emoteId) => choices.FirstOrDefault(c => c.EmoteId == emoteId)?.Variant;

    /// <summary>Haelt die Wahl fest; liefert die Mods, die neu zu setzen sind: erst die bisherige des Emotes, dann die neue.</summary>
    public IReadOnlyList<ModRef> Choose(uint emoteId, string emoteName, Variant variant)
    {
        var affected = new List<ModRef>();

        if (Release(emoteId) is { } previous)
            affected.Add(previous);

        choices.Add(new Choice(emoteId, emoteName, variant));

        if (!affected.Contains(variant.Mod))
            affected.Add(variant.Mod);

        return affected;
    }

    /// <summary>Nimmt die Wahl des Emotes heraus; liefert die Mod, die neu zu setzen oder zu entfernen ist.</summary>
    public ModRef? Release(uint emoteId)
    {
        var index = choices.FindIndex(c => c.EmoteId == emoteId);
        if (index < 0)
            return null;

        var mod = choices[index].Variant.Mod;
        choices.RemoveAt(index);
        return mod;
    }

    /// <summary>Die Wahlen aller Emotes in dieser Mod, in der Reihenfolge, in der sie getroffen wurden.</summary>
    public IReadOnlyList<Choice> ChoicesIn(ModRef mod) => choices.Where(c => c.Variant.Mod == mod).ToList();

    public void Clear() => choices.Clear();
}
