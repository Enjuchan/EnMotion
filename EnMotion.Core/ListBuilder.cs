namespace EnMotion.Core;

/// <summary>
/// Baut die Listen der Reiter: die gemeinsame Favoritenreihe und das Gitter eines
/// Reiters.
///
/// Getrennt vom Zeichnen, weil hier die Fragen sitzen, die man am Schreibtisch
/// beantworten will - etwa was mit einem Favoriten geschieht, dessen Mod gerade
/// abgeschaltet ist.
/// </summary>
public static class ListBuilder
{
    /// <summary>
    /// Alle angehefteten Emotes, Taenze und Ausdruecke in Anheftreihenfolge, soweit gerade
    /// gemoddet. Suche und Mod-Filter wirken hier bewusst nicht, damit die Reihe
    /// an ihrem Platz bleibt; nur <paramref name="ausblenden"/> wirkt.
    ///
    /// Ein Favorit, dessen Emote gerade nicht gemoddet ist, faellt heraus -
    /// gespeichert bleibt er trotzdem, das entscheidet <see cref="Favourites"/>.
    /// </summary>
    public static IReadOnlyList<ModdedEmote> Pinned(
        IEnumerable<ModdedEmote> alle,
        Favourites favoriten,
        Func<ModdedEmote, bool>? ausblenden = null)
    {
        var verfuegbar = alle
            .Where(m => ausblenden is null || !ausblenden(m))
            .ToList();

        // Die Reihe folgt der Favoritenliste, nicht der Emote-Liste - sonst
        // waere die gewaehlte Reihenfolge wirkungslos.
        return favoriten.Ids
            .Select(id => verfuegbar.FirstOrDefault(m => m.Emote.Id == id))
            .OfType<ModdedEmote>()
            .ToList();
    }

    /// <summary>
    /// Die nicht angehefteten Eintraege eines Reiters (<paramref name="art"/>). Leere <paramref name="modFilter"/> heisst alle
    /// Mods, mehrere Mods gelten als Vereinigung.
    /// </summary>
    public static IReadOnlyList<ModdedEmote> Grid(
        IEnumerable<ModdedEmote> alle,
        Favourites favoriten,
        EmoteKind art,
        string suche,
        IReadOnlySet<string> modFilter,
        Func<ModdedEmote, bool>? ausblenden = null)
    {
        return alle
            .Where(m => m.Emote.Kind == art)
            .Where(m => !favoriten.Contains(m.Emote.Id))
            .Where(m => modFilter.Count == 0 || m.Mods.Any(modFilter.Contains))
            .Where(m => Trifft(m, suche))
            .Where(m => ausblenden is null || !ausblenden(m))
            .ToList();
    }

    /// <summary>Die Mod-Namen der Eintraege eines Reiters, eindeutig und sortiert.</summary>
    public static IReadOnlyList<string> ModNames(IEnumerable<ModdedEmote> alle, EmoteKind art) =>
        alle
            .Where(m => m.Emote.Kind == art)
            .SelectMany(m => m.Mods)
            .Distinct()
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static bool Trifft(ModdedEmote m, string suche)
    {
        if (string.IsNullOrWhiteSpace(suche))
            return true;

        return m.Emote.Name.Contains(suche, StringComparison.CurrentCultureIgnoreCase)
            || m.Emote.TextCommand.Contains(suche, StringComparison.CurrentCultureIgnoreCase)
            || m.Mods.Any(mod => mod.Contains(suche, StringComparison.CurrentCultureIgnoreCase));
    }
}
