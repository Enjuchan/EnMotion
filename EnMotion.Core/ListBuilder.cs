namespace EnMotion.Core;

/// <summary>
/// Baut die Liste eines Reiters: Favoriten oben, der Rest darunter.
///
/// Getrennt vom Zeichnen, weil hier die Fragen sitzen, die man am Schreibtisch
/// beantworten will - etwa was mit einem Favoriten geschieht, dessen Mod gerade
/// abgeschaltet ist.
/// </summary>
public static class ListBuilder
{
    /// <summary>
    /// <paramref name="taenze"/> waehlt den Reiter: Taenze oder uebrige Emotes.
    ///
    /// Ein Favorit, dessen Emote gerade nicht gemoddet ist, faellt aus beiden Listen -
    /// gespeichert bleibt er trotzdem, das entscheidet <see cref="Favourites"/>.
    /// </summary>
    public static (IReadOnlyList<ModdedEmote> Pinned, IReadOnlyList<ModdedEmote> Others) Build(
        IEnumerable<ModdedEmote> alle,
        Favourites favoriten,
        string suche,
        bool taenze)
    {
        var passend = alle
            .Where(m => m.Emote.IsDance == taenze)
            .Where(m => Trifft(m, suche))
            .ToList();

        // Die angeheftete Reihe folgt der Favoritenliste, nicht der Emote-Liste -
        // sonst waere die gewaehlte Reihenfolge wirkungslos.
        var pinned = favoriten.Ids
            .Select(id => passend.FirstOrDefault(m => m.Emote.Id == id))
            .OfType<ModdedEmote>()
            .ToList();

        var rest = passend
            .Where(m => !favoriten.Contains(m.Emote.Id))
            .ToList();

        return (pinned, rest);
    }

    private static bool Trifft(ModdedEmote m, string suche)
    {
        if (string.IsNullOrWhiteSpace(suche))
            return true;

        return m.Emote.Name.Contains(suche, StringComparison.CurrentCultureIgnoreCase)
            || m.Emote.TextCommand.Contains(suche, StringComparison.CurrentCultureIgnoreCase);
    }
}
