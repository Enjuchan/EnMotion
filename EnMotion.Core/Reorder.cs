namespace EnMotion.Core;

/// <summary>
/// Verschieben in einer Liste, von der nur ein Teil sichtbar ist. Die Indizes zaehlen unter den
/// sichtbaren Eintraegen; die unsichtbaren behalten ihren Platz.
/// </summary>
public static class Reorder
{
    /// <summary>
    /// Setzt den sichtbaren Eintrag an Position <paramref name="von"/> so um, dass er danach an Position
    /// <paramref name="nach"/> unter den sichtbaren steht. Die sichtbaren Eintraege belegen danach dieselben
    /// Plaetze in der Liste wie vorher, nur in neuer Reihenfolge. Gibt zurueck, ob sich etwas geaendert hat;
    /// bei ungueltigen Indizes oder gleichem Index bleibt die Liste unveraendert.
    /// </summary>
    public static bool Move<T>(List<T> items, Func<T, bool> visible, int von, int nach)
    {
        var plaetze = new List<int>();

        for (var i = 0; i < items.Count; i++)
            if (visible(items[i]))
                plaetze.Add(i);

        if (von == nach || von < 0 || nach < 0 || von >= plaetze.Count || nach >= plaetze.Count)
            return false;

        var sichtbar = plaetze.Select(p => items[p]).ToList();
        var bewegt = sichtbar[von];

        sichtbar.RemoveAt(von);
        sichtbar.Insert(nach, bewegt);

        for (var k = 0; k < plaetze.Count; k++)
            items[plaetze[k]] = sichtbar[k];

        return true;
    }

    /// <summary>
    /// Liegt der Punkt im Rechteck, das alle Elemente einer Zeile umschliesst, erweitert um <paramref name="rand"/>
    /// an allen Seiten? Ausserhalb gilt ein Zug als abgebrochen.
    /// </summary>
    public static bool InRow(IReadOnlyCollection<(float MinX, float MinY, float MaxX, float MaxY)> elemente,
        float x, float y, float rand)
    {
        if (elemente.Count == 0)
            return false;

        return x >= elemente.Min(e => e.MinX) - rand && x <= elemente.Max(e => e.MaxX) + rand
            && y >= elemente.Min(e => e.MinY) - rand && y <= elemente.Max(e => e.MaxY) + rand;
    }

    /// <summary>Dauer der Gleitbewegung in Sekunden.</summary>
    public const float GlideSeconds = 0.14f;

    /// <summary>Ease-out (kubisch): Fortschritt 0 bis 1 aus der verstrichenen Zeit, begrenzt auf 0 bis 1.</summary>
    public static float Ease(float verstrichen, float dauer = GlideSeconds)
    {
        if (dauer <= 0f)
            return 1f;

        var t = Math.Clamp(verstrichen / dauer, 0f, 1f);
        var r = 1f - t;
        return 1f - r * r * r;
    }

    /// <summary>
    /// Die Reihenfolge der Elemente (Index vor dem Zug) nach dem Entfernen von <paramref name="von"/> und Einsetzen
    /// an <paramref name="luecke"/>. Ungueltige Werte ergeben die unveraenderte Reihenfolge.
    /// </summary>
    public static List<int> OrderDuringDrag(int anzahl, int von, int luecke)
    {
        var order = Enumerable.Range(0, anzahl).ToList();

        if (von < 0 || von >= anzahl || luecke < 0 || luecke >= anzahl)
            return order;

        order.RemoveAt(von);
        order.Insert(luecke, von);
        return order;
    }

    /// <summary>
    /// Die Zielplaetze waehrend eines Zugs: je Element (Index vor dem Zug) die linke obere Ecke, wenn das gezogene
    /// Element <paramref name="von"/> an <paramref name="luecke"/> landen wuerde. Die Elemente werden in der neuen
    /// Reihenfolge so umgebrochen wie beim Zeichnen: Ein Element bleibt in der Zeile, solange es samt Abstand vor
    /// <paramref name="rechts"/> endet.
    /// </summary>
    public static (float X, float Y)[] Positions(IReadOnlyList<float> breiten, int von, int luecke,
        float x0, float y0, float zeilenHoehe, float abstandX, float abstandY, float rechts)
    {
        var ergebnis = new (float X, float Y)[breiten.Count];
        var x = x0;
        var y = y0;
        var ende = x0;
        var erstes = true;

        foreach (var index in OrderDuringDrag(breiten.Count, von, luecke))
        {
            if (!erstes)
            {
                if (ende + abstandX + breiten[index] <= rechts)
                    x = ende + abstandX;
                else
                {
                    x = x0;
                    y += zeilenHoehe + abstandY;
                }
            }

            erstes = false;
            ergebnis[index] = (x, y);
            ende = x + breiten[index];
        }

        return ergebnis;
    }

    /// <summary>
    /// Der Platz, an dem das gezogene Element landet: das der Maus naechste Element und die Seite, auf der die
    /// Maus liegt (links der Mitte heisst davor). Das Ergebnis ist der Index nach dem Verschieben.
    /// </summary>
    public static int DropIndex(IReadOnlyList<(float MinX, float MinY, float MaxX, float MaxY)> rects,
        float x, float y, int von)
    {
        if (rects.Count == 0)
            return von;

        var ziel = 0;
        var kleinster = float.MaxValue;

        for (var i = 0; i < rects.Count; i++)
        {
            var r = rects[i];
            var dx = Math.Max(Math.Max(r.MinX - x, x - r.MaxX), 0f);
            var dy = Math.Max(Math.Max(r.MinY - y, y - r.MaxY), 0f);
            var abstand = dx * dx + dy * dy;

            if (abstand < kleinster)
            {
                kleinster = abstand;
                ziel = i;
            }
        }

        var davor = x < (rects[ziel].MinX + rects[ziel].MaxX) / 2f;
        var platz = davor ? ziel : ziel + 1;
        return platz > von ? platz - 1 : platz;
    }
}
