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

    public void Toggle(uint emoteId)
    {
        if (!ids.Remove(emoteId))
            ids.Add(emoteId);
    }

    /// <summary>Beim Laden aus der Konfiguration. Doppelte fallen weg.</summary>
    public void Load(IEnumerable<uint> gespeichert)
    {
        ids.Clear();

        foreach (var id in gespeichert)
            if (!ids.Contains(id))
                ids.Add(id);
    }
}
