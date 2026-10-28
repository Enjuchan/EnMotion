using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class VariantBookTests
{
    private static readonly ModRef A = new("dirA", "Mod A");
    private static readonly ModRef B = new("dirB", "Mod B");

    private static Variant Opt(ModRef mod, string option) => new(mod, "Gruppe", option);

    [Fact]
    public void Erste_Wahl_betrifft_nur_die_gewaehlte_Mod()
    {
        var book = new VariantBook();

        Assert.Equal([A], book.Choose(1, "Wave", Opt(A, "o1")));
        Assert.Equal(Opt(A, "o1"), book.ChosenFor(1));
    }

    [Fact]
    public void Wechsel_der_Variante_betrifft_alte_und_neue_Mod()
    {
        var book = new VariantBook();
        book.Choose(1, "Wave", Opt(A, "o1"));

        Assert.Equal([A, B], book.Choose(1, "Wave", Opt(B, "o2")));
        Assert.Empty(book.ChoicesIn(A));
        Assert.Single(book.ChoicesIn(B));
    }

    [Fact]
    public void Wechsel_innerhalb_derselben_Mod_betrifft_sie_einmal()
    {
        var book = new VariantBook();
        book.Choose(1, "Wave", Opt(A, "o1"));

        Assert.Equal([A], book.Choose(1, "Wave", Opt(A, "o2")));
        Assert.Equal(Opt(A, "o2"), Assert.Single(book.ChoicesIn(A)).Variant);
    }

    [Fact]
    public void Mehrere_Emotes_einer_Mod_werden_zusammengefuehrt()
    {
        var book = new VariantBook();
        book.Choose(1, "Wave", Opt(A, "o1"));
        book.Choose(2, "Pet", Opt(A, "o2"));

        Assert.Equal([1u, 2u], book.ChoicesIn(A).Select(c => c.EmoteId));
    }

    [Fact]
    public void Zuruecksetzen_eines_von_mehreren_laesst_die_Wahl_des_anderen()
    {
        var book = new VariantBook();
        book.Choose(1, "Wave", Opt(A, "o1"));
        book.Choose(2, "Pet", Opt(A, "o2"));

        Assert.Equal(A, book.Release(1));

        var rest = Assert.Single(book.ChoicesIn(A));
        Assert.Equal(2u, rest.EmoteId);
        Assert.Null(book.ChosenFor(1));
    }

    [Fact]
    public void Zuruecksetzen_des_letzten_leert_die_Mod()
    {
        var book = new VariantBook();
        book.Choose(1, "Wave", Opt(A, "o1"));

        Assert.Equal(A, book.Release(1));
        Assert.Empty(book.ChoicesIn(A));
    }

    [Fact]
    public void Zuruecksetzen_ohne_Wahl_und_Leeren()
    {
        var book = new VariantBook();

        Assert.Null(book.Release(7));

        book.Choose(1, "Wave", new Variant(A));
        book.Clear();

        Assert.Null(book.ChosenFor(1));
    }
}
