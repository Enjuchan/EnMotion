namespace EnMotion.Core;

/// <summary>
/// Ein Emote aus den Spieldaten, so weit wir es fuer die Liste brauchen.
///
/// <paramref name="AnimationKeys"/> sind die Pfadfragmente der zugehoerigen
/// Animationen. Der vollstaendige Spielpfad haengt zusaetzlich am Charaktermodell.
/// </summary>
public sealed record EmoteEntry(
    uint Id,
    string Name,
    uint IconId,
    string TextCommand,
    IReadOnlyList<string> AnimationKeys)
{
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
    public bool IsDance => AnimationKeys.Any(k => k.StartsWith("emote/dance", StringComparison.Ordinal));
}
