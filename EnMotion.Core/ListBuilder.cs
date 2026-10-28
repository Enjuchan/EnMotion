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
    /// Ein Favorit, dessen Emote gerade nicht gemoddet ist, bleibt in der Reihe, wenn
    /// <paramref name="alleEmotes"/> gegeben ist: Er erscheint dann als ungemoddet (ohne Mods).
    /// Ohne <paramref name="alleEmotes"/> faellt er heraus; gespeichert bleibt er in beiden
    /// Faellen, das entscheidet <see cref="Favourites"/>.
    /// </summary>
    public static IReadOnlyList<ModdedEmote> Pinned(
        IEnumerable<ModdedEmote> alle,
        Favourites favoriten,
        Func<ModdedEmote, bool>? ausblenden = null,
        IEnumerable<EmoteEntry>? alleEmotes = null)
    {
        var verfuegbar = alle
            .Where(m => ausblenden is null || !ausblenden(m))
            .ToList();

        if (alleEmotes is not null)
        {
            var gemoddetIds = alle.Select(m => m.Emote.Id).ToHashSet();
            var ungemoddet = alleEmotes
                .Where(e => favoriten.Contains(e.Id) && !gemoddetIds.Contains(e.Id))
                .GroupBy(e => e.Id)
                .Select(g => new ModdedEmote(g.First(), []))
                .Where(m => ausblenden is null || !ausblenden(m));

            verfuegbar.AddRange(ungemoddet);
        }

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

    /// <summary>Ein Emote in der Auswahl zum Hinzufuegen; <paramref name="Favourite"/> heisst, es ist schon Favorit.</summary>
    public sealed record AddCandidate(EmoteEntry Emote, bool Favourite);

    /// <summary>
    /// Die Auswahl fuer die Plus-Kachel: alle freigeschalteten Emotes, deren Name oder Befehl die Suche
    /// enthaelt (ohne Gross-/Kleinschreibung; leere Suche heisst alle), nach Namen sortiert. Bereits
    /// favorisierte bleiben drin und sind markiert.
    /// </summary>
    public static IReadOnlyList<AddCandidate> AddCandidates(
        IEnumerable<EmoteEntry> alle,
        Favourites favoriten,
        string suche,
        Func<EmoteEntry, bool> freigeschaltet)
    {
        var begriff = suche.Trim();

        return alle
            .Where(freigeschaltet)
            .Where(e => begriff.Length == 0
                || e.Name.Contains(begriff, StringComparison.OrdinalIgnoreCase)
                || e.TextCommand.Contains(begriff, StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => new AddCandidate(e, favoriten.Contains(e.Id)))
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
