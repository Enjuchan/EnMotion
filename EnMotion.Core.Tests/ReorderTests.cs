using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class ReorderTests
{
    private static List<int> Liste(params int[] w) => [.. w];

    [Fact]
    public void Verschiebt_nach_hinten_und_nach_vorn()
    {
        var l = Liste(1, 2, 3, 4);
        Assert.True(Reorder.Move(l, _ => true, 0, 2));
        Assert.Equal([2, 3, 1, 4], l);

        Assert.True(Reorder.Move(l, _ => true, 3, 1));
        Assert.Equal([2, 4, 3, 1], l);
    }

    [Fact]
    public void Verschieben_an_Anfang_und_Ende()
    {
        var l = Liste(1, 2, 3);
        Assert.True(Reorder.Move(l, _ => true, 2, 0));
        Assert.Equal([3, 1, 2], l);

        Assert.True(Reorder.Move(l, _ => true, 0, 2));
        Assert.Equal([1, 2, 3], l);
    }

    [Fact]
    public void Gleicher_Index_aendert_nichts()
    {
        var l = Liste(1, 2, 3);
        Assert.False(Reorder.Move(l, _ => true, 1, 1));
        Assert.Equal([1, 2, 3], l);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void Ungueltige_Indizes_aendern_nichts(int von, int nach)
    {
        var l = Liste(1, 2, 3);
        Assert.False(Reorder.Move(l, _ => true, von, nach));
        Assert.Equal([1, 2, 3], l);
    }

    [Fact]
    public void Leere_Liste_aendert_nichts()
    {
        var l = Liste();
        Assert.False(Reorder.Move(l, _ => true, 0, 1));
    }

    [Fact]
    public void Unsichtbare_Eintraege_behalten_ihren_Platz()
    {
        // Sichtbar sind die ungeraden; 2 und 4 bleiben an Platz 1 und 3.
        var l = Liste(1, 2, 3, 4, 5);
        Assert.True(Reorder.Move(l, x => x % 2 == 1, 0, 2));
        Assert.Equal([3, 2, 5, 4, 1], l);
    }

    [Fact]
    public void Index_zaehlt_nur_unter_den_sichtbaren()
    {
        var l = Liste(1, 2, 3);
        Assert.False(Reorder.Move(l, x => x != 2, 0, 2));
        Assert.Equal([1, 2, 3], l);
    }

    [Fact]
    public void Favoriten_verschieben_und_Ausgeblendete_bleiben()
    {
        var f = new Favourites();
        f.Load([10u, 20u, 30u, 40u]);

        Assert.True(f.Move(id => id != 20, 0, 2));
        Assert.Equal([30u, 20u, 40u, 10u], f.Ids);
    }

    [Fact]
    public void Posen_mehrerer_Haltungen_nur_die_gewaehlte_wird_umgestellt()
    {
        var p = new PoseFavourites();
        p.Load(
        [
            new() { Stance = 0, Index = 1 },
            new() { Stance = 1, Index = 5 },
            new() { Stance = 0, Index = 2 },
            new() { Stance = 1, Index = 6 },
            new() { Stance = 0, Index = 3 },
        ]);

        Assert.True(p.Move(0, _ => true, 0, 2));

        Assert.Equal(
            [(byte)0, (byte)1, (byte)0, (byte)1, (byte)0],
            p.Items.Select(i => i.Stance));
        Assert.Equal([(byte)2, (byte)3, (byte)1], p.IndicesOf(0));
        Assert.Equal([(byte)5, (byte)6], p.IndicesOf(1));
    }

    [Fact]
    public void Posen_mit_unsichtbarem_Index_behalten_ihren_Platz()
    {
        var p = new PoseFavourites();
        p.Load(
        [
            new() { Stance = 0, Index = 1 },
            new() { Stance = 0, Index = 9 },
            new() { Stance = 0, Index = 2 },
        ]);

        Assert.True(p.Move(0, i => i < 5, 0, 1));
        Assert.Equal([(byte)2, (byte)9, (byte)1], p.IndicesOf(0));
    }

    private static readonly (float, float, float, float)[] Zeile =
        [(0f, 0f, 10f, 10f), (20f, 0f, 30f, 10f), (0f, 20f, 10f, 30f)];

    [Theory]
    [InlineData(5f, 5f, true)]
    [InlineData(25f, 25f, true)]
    [InlineData(-4f, 35f, true)]
    [InlineData(-6f, 5f, false)]
    [InlineData(35.1f, 5f, false)]
    [InlineData(5f, -5.1f, false)]
    [InlineData(5f, 40f, false)]
    public void Treffer_im_Zeilenrechteck_mit_Rand(float x, float y, bool erwartet) =>
        Assert.Equal(erwartet, Reorder.InRow(Zeile, x, y, 5f));

    [Fact]
    public void Leere_Zeile_trifft_nie() => Assert.False(Reorder.InRow([], 0f, 0f, 5f));
}
