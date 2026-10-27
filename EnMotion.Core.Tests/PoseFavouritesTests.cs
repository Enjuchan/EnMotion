using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class PoseFavouritesTests
{
    private static PoseFavouriteEntry E(byte stance, byte index) => new() { Stance = stance, Index = index };

    [Fact]
    public void Umschalten_zweimal_loest_wieder()
    {
        var f = new PoseFavourites();

        f.Toggle(1, 3);
        Assert.True(f.Contains(1, 3));

        f.Toggle(1, 3);
        Assert.False(f.Contains(1, 3));
        Assert.Empty(f.Items);
    }

    [Fact]
    public void Gleicher_Index_in_anderer_Haltung_ist_eine_andere_Pose()
    {
        var f = new PoseFavourites();
        f.Toggle(1, 2);

        Assert.False(f.Contains(2, 2));
    }

    [Fact]
    public void Reihenfolge_bleibt_die_des_Anheftens()
    {
        var f = new PoseFavourites();
        f.Toggle(2, 1);
        f.Toggle(1, 4);
        f.Toggle(1, 0);

        Assert.Equal([((byte)2, (byte)1), (1, 4), (1, 0)], f.Items);
    }

    [Fact]
    public void Eintraege_einer_Haltung_in_Anheftreihenfolge()
    {
        var f = new PoseFavourites();
        f.Toggle(1, 4);
        f.Toggle(2, 1);
        f.Toggle(1, 0);

        Assert.Equal([(byte)4, (byte)0], f.IndicesOf(1));
        Assert.Empty(f.IndicesOf(3));
    }

    [Fact]
    public void Doppelte_fallen_beim_Laden_weg()
    {
        var f = new PoseFavourites();

        f.Load([E(1, 2), E(1, 2), E(2, 2)]);

        Assert.Equal([((byte)1, (byte)2), (2, 2)], f.Items);
    }

    [Fact]
    public void Laden_ersetzt_den_bisherigen_Stand()
    {
        var f = new PoseFavourites();
        f.Toggle(9, 9);

        f.Load([E(1, 1)]);

        Assert.Equal([((byte)1, (byte)1)], f.Items);
    }

    [Fact]
    public void Eintraege_lassen_sich_speichern_und_wieder_laden()
    {
        var f = new PoseFavourites();
        f.Toggle(2, 1);
        f.Toggle(1, 5);

        var g = new PoseFavourites();
        g.Load(f.ToEntries());

        Assert.Equal(f.Items, g.Items);
    }
}
