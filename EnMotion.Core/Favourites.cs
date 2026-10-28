namespace EnMotion.Core;

/// <summary>
/// Die angehefteten Emotes.
///
/// Die Reihenfolge ist die des Hinzufuegens. Alphabetisch zu sortieren waere
/// naheliegend und falsch: Die angeheftete Reihe soll an derselben Stelle bleiben,
/// damit man sie blind trifft.
///
/// Nur Emotes und Taenze - Posen haben keine Nummer in diesem Sinn, sondern eine
/// Haltung und einen Index darin.
/// </summary>
public sealed class Favourites
{
    private readonly List<uint> ids = [];

    public IReadOnlyList<uint> Ids => ids;

    public bool Contains(uint emoteId) => ids.Contains(emoteId);

    /// <summary>Haengt das Emote ans Ende an. Gibt es schon, aendert sich nichts.</summary>
    public bool Add(uint emoteId)
    {
        if (ids.Contains(emoteId))
            return false;

        ids.Add(emoteId);
        return true;
    }

    public void Toggle(uint emoteId)
    {
        if (!ids.Remove(emoteId))
            ids.Add(emoteId);
    }

    /// <summary>
    /// Verschiebt unter den sichtbaren Favoriten; unsichtbare (etwa ausgeblendete) behalten ihren Platz.
    /// </summary>
    public bool Move(Func<uint, bool> sichtbar, int von, int nach) => Reorder.Move(ids, sichtbar, von, nach);

    /// <summary>Beim Laden aus der Konfiguration. Doppelte fallen weg.</summary>
    public void Load(IEnumerable<uint> gespeichert)
    {
        ids.Clear();

        foreach (var id in gespeichert)
            if (!ids.Contains(id))
                ids.Add(id);
    }
}
