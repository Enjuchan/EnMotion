namespace EnMotion.Core;

/// <summary>
/// Gespeicherte angeheftete Pose. Eigener Typ, damit die Konfiguration ihn
/// serialisieren kann; die Haltung ist ein byte, weil Core den Enum des Spiels
/// nicht kennt.
/// </summary>
public sealed class PoseFavouriteEntry
{
    public byte Stance { get; set; }
    public byte Index { get; set; }
}

/// <summary>
/// Die angehefteten Posen, in der Reihenfolge des Anheftens - aus demselben Grund
/// wie bei <see cref="Favourites"/>: Die Reihe soll blind trefferbar bleiben.
/// </summary>
public sealed class PoseFavourites
{
    private readonly List<(byte Stance, byte Index)> items = [];

    public IReadOnlyList<(byte Stance, byte Index)> Items => items;

    public bool Contains(byte stance, byte index) => items.Contains((stance, index));

    public void Toggle(byte stance, byte index)
    {
        if (!items.Remove((stance, index)))
            items.Add((stance, index));
    }

    /// <summary>Die Indizes der angehefteten Posen einer Haltung, in Anheftreihenfolge.</summary>
    public IReadOnlyList<byte> IndicesOf(byte stance) =>
        items.Where(p => p.Stance == stance).Select(p => p.Index).ToList();

    /// <summary>Beim Laden aus der Konfiguration. Doppelte fallen weg.</summary>
    public void Load(IEnumerable<PoseFavouriteEntry> gespeichert)
    {
        items.Clear();

        foreach (var e in gespeichert)
            if (!items.Contains((e.Stance, e.Index)))
                items.Add((e.Stance, e.Index));
    }

    public List<PoseFavouriteEntry> ToEntries() =>
        items.Select(p => new PoseFavouriteEntry { Stance = p.Stance, Index = p.Index }).ToList();
}
