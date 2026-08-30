using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class FavouritesTests
{
    [Fact]
    public void Umschalten_fuegt_hinzu_und_entfernt_wieder()
    {
        var f = new Favourites();

        f.Toggle(105);
        Assert.True(f.Contains(105));

        f.Toggle(105);
        Assert.False(f.Contains(105));
    }

    /// <summary>
    /// Die Reihenfolge ist die des Hinzufuegens, nicht alphabetisch: Wer zuerst
    /// markiert, hat es meist am noetigsten - und eine Reihe, die sich selbst
    /// umsortiert, taugt nicht fuers Muskelgedaechtnis.
    /// </summary>
    [Fact]
    public void Reihenfolge_bleibt_die_des_Hinzufuegens()
    {
        var f = new Favourites();

        f.Toggle(146);
        f.Toggle(105);
        f.Toggle(112);

        Assert.Equal([146u, 105u, 112u], f.Ids);
    }

    [Fact]
    public void Doppeltes_Hinzufuegen_gibt_es_nicht()
    {
        var f = new Favourites();

        f.Toggle(105);
        f.Load([105, 105, 146]);

        Assert.Equal([105u, 146u], f.Ids);
    }

    [Fact]
    public void Entfernen_laesst_die_uebrige_Reihenfolge_stehen()
    {
        var f = new Favourites();
        f.Load([146, 105, 112]);

        f.Toggle(105);

        Assert.Equal([146u, 112u], f.Ids);
    }

    [Fact]
    public void Laden_ersetzt_den_bisherigen_Stand()
    {
        var f = new Favourites();
        f.Toggle(999);

        f.Load([105]);

        Assert.Equal([105u], f.Ids);
    }
}
