namespace EnMotion.Core;

/// <summary>
/// Ein Emote aus den Spieldaten, so weit wir es fuer die Liste brauchen.
///
/// <paramref name="AnimationKeys"/> sind die Pfadfragmente der zugehoerigen
/// Animationen. Der vollstaendige Spielpfad haengt zusaetzlich am Charaktermodell.
/// <paramref name="CategoryRow"/> ist die Zeilennummer in der Tabelle EmoteCategory.
/// </summary>
public sealed record EmoteEntry(
    uint Id,
    string Name,
    uint IconId,
    string TextCommand,
    IReadOnlyList<string> AnimationKeys,
    uint CategoryRow)
{
    /// <summary>
    /// Zeile der Gesichtsausdruecke in EmoteCategory (1 General, 2 Special, 3 Expressions;
    /// Quelle: xivapi/ffxiv-datamining, csv/en/EmoteCategory.csv). Die Zeilennummer ist
    /// sprachunabhaengig, im Gegensatz zum Namen.
    /// </summary>
    public const uint ExpressionCategoryRow = 3;

    /// <summary>
    /// Ob es sich um einen Tanz handelt.
    ///
    /// Die Emote-Kategorie taugt dafuer nicht: Taenze liegen dort unter "Speziell",
    /// zusammen mit Sitzen, Doesen und allem anderen Sonderbaren. Der Animationspfad
    /// dagegen ist eindeutig - alle 25 Taenze beginnen mit "emote/dance", und nichts
    /// anderes tut das. Ausserdem ist er sprachunabhaengig, im Gegensatz zum Namen.
    ///
    /// Einzige Ausnahme ist der Prinzessinnentanz unter "emote_sp/sp44"; er bleibt bei
    /// den Emotes. Ein Sonderfall im Code waere den einen Eintrag nicht wert.
    /// </summary>
    private bool IsDance => AnimationKeys.Any(k => k.StartsWith("emote/dance", StringComparison.Ordinal));

    /// <summary>Der Reiter des Eintrags. Der Tanz hat Vorrang vor der Kategorie.</summary>
    public EmoteKind Kind =>
        IsDance ? EmoteKind.Dance
        : CategoryRow == ExpressionCategoryRow ? EmoteKind.Expression
        : EmoteKind.Emote;
}

/// <summary>Die drei Reiter, auf die sich Emotes verteilen. Die Reihenfolge ist die der Reiter.</summary>
public enum EmoteKind { Emote, Dance, Expression }
