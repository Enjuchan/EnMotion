using System.Numerics;
using Dalamud.Bindings.ImGui;
using EnMotion.Core;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>
/// Sortieren der Favoritenreihen per Strg+Ziehen. Jede Reihe sortiert nur in sich. Ohne ImGui-Drag-and-drop,
/// weil die Reihen ihre Elemente selbst als InvisibleButton zeichnen und nur der Zustand gebraucht wird.
///
/// Der Knopf jedes Elements bleibt an seinem Platz im Layout; gezeichnet wird mit einem Versatz. Der Versatz des
/// gezogenen Elements folgt der Maus, die der anderen gleitet zeitbasiert auf die Plätze, die sie bei der
/// aktuellen Lücke hätten (<see cref="Reorder.Positions"/>). Die Plätze eines Frames stammen aus dem Layout des
/// vorigen, das sich während eines Zugs nicht ändert.
/// </summary>
public sealed partial class MainWindow
{
    private const float ZugAlpha = 0.4f;

    private sealed class Gleiter
    {
        public Vector2 Start;
        public Vector2 Ziel;
        public Vector2 Jetzt;
        public float T;
    }

    private sealed class ZugZeile
    {
        public List<(Vector2 Min, Vector2 Max)> Rects = [];
        public List<long> Keys = [];
        public List<(Vector2 Min, Vector2 Max)> Neu = [];
        public List<long> KeysNeu = [];
        public Dictionary<long, Gleiter> Gleiter = [];
    }

    private readonly Dictionary<string, ZugZeile> zugZeilen = [];
    private string? zugReihe;
    private int zugVon;
    private long zugKey;
    private Vector2 zugGriff;
    private int zugFrame;
    private bool zugAbgebrochen;

    /// <summary>Läuft gerade ein Zug? Dann zeigen Kacheln und Chips keinen Tooltip.</summary>
    private bool Zieht => zugReihe is not null;

    private static string SortierHinweis => Loc.T("Hold Ctrl and drag to reorder", "Strg gedrückt halten und ziehen zum Sortieren");

    private ZugZeile Zeile(string reihe)
    {
        if (!zugZeilen.TryGetValue(reihe, out var z))
            zugZeilen[reihe] = z = new ZugZeile();

        return z;
    }

    private static uint Abdunkeln(Vector4 farbe, float alpha) => Theme.U32(farbe with { W = farbe.W * alpha });

    private static (float, float, float, float) Tupel((Vector2 Min, Vector2 Max) r) => (r.Min.X, r.Min.Y, r.Max.X, r.Max.Y);

    /// <summary>Ob der Zug gerade gilt: nicht abgebrochen und die Maus in der Zeile (mit halber Elementhöhe Rand).</summary>
    private bool ZugGueltig(List<(Vector2 Min, Vector2 Max)> rects)
    {
        if (zugAbgebrochen || rects.Count == 0)
            return false;

        var maus = ImGui.GetMousePos();
        var rand = (rects[0].Max.Y - rects[0].Min.Y) / 2f;
        return Reorder.InRow(rects.Select(Tupel).ToList(), maus.X, maus.Y, rand);
    }

    /// <summary>Vor dem ersten Element einer sortierbaren Reihe: Gleitbewegungen dieses Frames berechnen.</summary>
    private void ZugBeginn(string reihe)
    {
        var z = Zeile(reihe);
        z.Neu.Clear();
        z.KeysNeu.Clear();

        // Ein Zug, dessen Reihe nicht mehr gezeichnet wurde (Tab-Wechsel mitten im Ziehen), endet ohne Verschieben.
        if (zugReihe is not null && ImGui.GetFrameCount() - zugFrame > 1)
            zugReihe = null;

        var dt = Math.Min(ImGui.GetIO().DeltaTime, 0.1f);
        var anzahl = z.Keys.Count;
        var ziele = new Vector2[anzahl];
        var folgtMaus = false;

        if (zugReihe == reihe && z.Rects.Count == anzahl && zugVon < anzahl)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                zugAbgebrochen = true;

            var maus = ImGui.GetMousePos();
            var luecke = zugVon;

            if (ZugGueltig(z.Rects))
                luecke = Reorder.DropIndex(z.Rects.Select(Tupel).ToList(), maus.X, maus.Y, zugVon);

            var plaetze = Plaetze(z.Rects, luecke);

            for (var i = 0; i < anzahl; i++)
                ziele[i] = new Vector2(plaetze[i].X, plaetze[i].Y) - z.Rects[i].Min;

            folgtMaus = !zugAbgebrochen;

            if (folgtMaus)
            {
                var g = HoleGleiter(z, z.Keys[zugVon]);
                g.Jetzt = g.Start = g.Ziel = maus - zugGriff - z.Rects[zugVon].Min;
                g.T = Reorder.GlideSeconds;
            }
        }

        for (var i = 0; i < anzahl; i++)
        {
            if (folgtMaus && i == zugVon)
                continue;

            var key = z.Keys[i];

            if (!z.Gleiter.TryGetValue(key, out var g))
            {
                if (ziele[i] == Vector2.Zero)
                    continue;

                g = HoleGleiter(z, key);
            }

            if (g.Ziel != ziele[i])
            {
                g.Start = g.Jetzt;
                g.Ziel = ziele[i];
                g.T = 0f;
            }

            g.T += dt;
            g.Jetzt = g.Start + (g.Ziel - g.Start) * Reorder.Ease(g.T);

            if (g.T >= Reorder.GlideSeconds && g.Ziel == Vector2.Zero)
                z.Gleiter.Remove(key);
        }

        if (z.Gleiter.Count > anzahl)
        {
            var vorhanden = z.Keys.ToHashSet();

            foreach (var k in z.Gleiter.Keys.Where(k => !vorhanden.Contains(k)).ToList())
                z.Gleiter.Remove(k);
        }
    }

    private static Gleiter HoleGleiter(ZugZeile z, long key)
    {
        if (!z.Gleiter.TryGetValue(key, out var g))
            z.Gleiter[key] = g = new Gleiter();

        return g;
    }

    /// <summary>Die Plätze (linke obere Ecke) aller Elemente der Zeile, wenn die Lücke bei <paramref name="luecke"/> liegt.</summary>
    private (float X, float Y)[] Plaetze(List<(Vector2 Min, Vector2 Max)> rects, int luecke)
    {
        var style = ImGui.GetStyle();
        var rechts = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;

        return Reorder.Positions(rects.Select(r => r.Max.X - r.Min.X).ToList(), zugVon, luecke,
            rects[0].Min.X, rects[0].Min.Y, rects[0].Max.Y - rects[0].Min.Y,
            style.ItemSpacing.X, style.ItemSpacing.Y, rechts);
    }

    /// <summary>Der Versatz, mit dem das Element mit diesem Schlüssel gezeichnet wird.</summary>
    private Vector2 Versatz(string reihe, long key) =>
        Zeile(reihe).Gleiter.TryGetValue(key, out var g) ? g.Jetzt : Vector2.Zero;

    private bool IstGezogen(string reihe, long key) => zugReihe == reihe && zugKey == key;

    /// <summary>
    /// Direkt nach dem Knopf eines Elements aufrufen. Beginnt den Zug, wenn Strg gehalten und gezogen wird, und
    /// sagt, ob der Klick auslösen darf: nach einem Zug nicht, auch wenn die Maus beim Loslassen über dem Element steht.
    /// </summary>
    private bool ZugPruefen(string reihe, int index, long key, bool geklickt)
    {
        var z = Zeile(reihe);
        var min = ImGui.GetItemRectMin();

        z.Neu.Add((min, ImGui.GetItemRectMax()));
        z.KeysNeu.Add(key);

        if (zugReihe is null && ImGui.IsItemActive() && ImGui.GetIO().KeyCtrl
            && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4f))
        {
            zugReihe = reihe;
            zugVon = index;
            zugKey = key;
            zugGriff = ImGui.GetMousePos() - min;
            zugAbgebrochen = false;
            zugFrame = ImGui.GetFrameCount();
        }

        return geklickt && zugReihe is null;
    }

    /// <summary>
    /// Nach dem letzten Element der Reihe. Beim Loslassen in der Zeile wird verschoben und das Element gleitet in
    /// die Lücke; sonst gleiten alle zurück. <paramref name="verschieben"/> bekommt die Position vor und nach dem
    /// Verschieben unter den gezeigten Elementen.
    /// </summary>
    private void ZugEnde(string reihe, Action<int, int> verschieben)
    {
        var z = Zeile(reihe);

        if (zugReihe == reihe)
        {
            zugFrame = ImGui.GetFrameCount();

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                zugAbgebrochen = true;

            if (z.Neu.Count == 0 || zugVon >= z.Neu.Count)
                zugReihe = null;
            else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                zugReihe = null;

                if (ZugGueltig(z.Neu))
                {
                    var maus = ImGui.GetMousePos();
                    var luecke = Reorder.DropIndex(z.Neu.Select(Tupel).ToList(), maus.X, maus.Y, zugVon);

                    if (luecke != zugVon)
                    {
                        Einrasten(z, luecke);
                        verschieben(zugVon, luecke);
                    }
                }
            }
        }

        (z.Rects, z.Neu) = (z.Neu, z.Rects);
        (z.Keys, z.KeysNeu) = (z.KeysNeu, z.Keys);
    }

    /// <summary>
    /// Nach dem Verschieben liegt jedes Element auf seinem neuen Platz im Layout. Damit nichts springt, startet es
    /// dort mit dem Versatz, der es optisch an seiner bisherigen Stelle hält, und gleitet auf null.
    /// </summary>
    private void Einrasten(ZugZeile z, int luecke)
    {
        var plaetze = Plaetze(z.Neu, luecke);

        for (var i = 0; i < z.Neu.Count; i++)
        {
            var key = z.KeysNeu[i];
            var jetzt = z.Gleiter.TryGetValue(key, out var alt) ? alt.Jetzt : Vector2.Zero;
            var sichtbar = z.Neu[i].Min + jetzt;
            var versatz = sichtbar - new Vector2(plaetze[i].X, plaetze[i].Y);
            var g = HoleGleiter(z, key);

            g.Jetzt = g.Start = versatz;
            g.Ziel = Vector2.Zero;
            g.T = 0f;
        }
    }
}
