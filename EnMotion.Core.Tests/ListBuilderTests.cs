using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class ListBuilderTests
{
    private const uint Allgemein = 1;
    private const uint Speziell = 2;
    private const uint Ausdruecke = EmoteEntry.ExpressionCategoryRow;

    private static ModdedEmote Emote(uint id, string name, string command, bool tanz = false, string mod = "irgendein Mod",
        uint kategorie = Allgemein) =>
        new(new EmoteEntry(id, name, 0, command, [tanz ? "emote/dance01" : "emote/norm01"], kategorie),
            [mod]);

    private static readonly ModdedEmote Pet = Emote(105, "Pet", "/stroke");
    private static readonly ModdedEmote Dote = Emote(146, "Dote", "/dote");
    private static readonly ModdedEmote Hug = Emote(112, "Hug", "/hug");
    private static readonly ModdedEmote Dance = Emote(20, "Ballroom", "/ballroom", tanz: true, kategorie: Speziell);
    private static readonly ModdedEmote Smile = Emote(69, "Smile", "/smile", kategorie: Ausdruecke);

    private static readonly IReadOnlySet<string> KeinFilter = new HashSet<string>();

    private static IReadOnlySet<string> Filter(params string[] mods) => new HashSet<string>(mods);

    private static Favourites Mit(params uint[] ids)
    {
        var f = new Favourites();
        f.Load(ids);
        return f;
    }

    private static IReadOnlyList<ModdedEmote> Gitter(
        IEnumerable<ModdedEmote> alle, Favourites f, string suche = "", EmoteKind art = EmoteKind.Emote,
        IReadOnlySet<string>? filter = null, Func<ModdedEmote, bool>? aus = null) =>
        ListBuilder.Grid(alle, f, art, suche, filter ?? KeinFilter, aus);

    [Fact]
    public void Favoriten_stehen_in_der_Reihe_der_Rest_im_Gitter()
    {
        var f = Mit(146);

        Assert.Equal([Dote], ListBuilder.Pinned([Pet, Dote, Hug], f));
        Assert.Equal([Pet, Hug], Gitter([Pet, Dote, Hug], f));
    }

    /// <summary>
    /// Die Reihe folgt der Favoritenliste, nicht der Emote-Liste - sonst waere die
    /// gewaehlte Reihenfolge wirkungslos.
    /// </summary>
    [Fact]
    public void Angeheftete_folgen_der_Favoritenreihenfolge()
    {
        Assert.Equal([Hug, Pet], ListBuilder.Pinned([Pet, Dote, Hug], Mit(112, 105)));
    }

    [Fact]
    public void Reihe_mischt_Emotes_und_Taenze_in_Anheftreihenfolge()
    {
        Assert.Equal([Hug, Dance, Pet], ListBuilder.Pinned([Pet, Dance, Hug], Mit(112, 20, 105)));
    }

    [Fact]
    public void Reihe_mischt_alle_drei_Einordnungen()
    {
        Assert.Equal([Smile, Dance, Pet], ListBuilder.Pinned([Pet, Dance, Smile], Mit(69, 20, 105)));
    }

    [Fact]
    public void Emotes_Taenze_und_Ausdruecke_werden_im_Gitter_getrennt()
    {
        var f = new Favourites();
        var alle = new[] { Pet, Dance, Smile };

        Assert.Equal([Pet], Gitter(alle, f, art: EmoteKind.Emote));
        Assert.Equal([Dance], Gitter(alle, f, art: EmoteKind.Dance));
        Assert.Equal([Smile], Gitter(alle, f, art: EmoteKind.Expression));
    }

    [Fact]
    public void Kategorie_Speziell_ist_ein_Emote_Ausdruecke_sind_keine_Emotes()
    {
        var sitzen = Emote(50, "Sit", "/sit", kategorie: Speziell);

        Assert.Equal(EmoteKind.Emote, sitzen.Emote.Kind);
        Assert.Equal(EmoteKind.Emote, Pet.Emote.Kind);
        Assert.Equal(EmoteKind.Expression, Smile.Emote.Kind);
        Assert.Equal(EmoteKind.Emote, Emote(51, "Unbekannt", "/x", kategorie: 0).Emote.Kind);
    }

    /// <summary>Der Animationspfad hat Vorrang: Ein Tanz mit Ausdrucks-Kategorie bleibt ein Tanz.</summary>
    [Fact]
    public void Tanz_mit_Ausdrucks_Kategorie_bleibt_Tanz()
    {
        var tanz = Emote(60, "Dance in Face", "/df", tanz: true, kategorie: Ausdruecke);
        var f = new Favourites();

        Assert.Equal(EmoteKind.Dance, tanz.Emote.Kind);
        Assert.Equal([tanz], Gitter([tanz], f, art: EmoteKind.Dance));
        Assert.Empty(Gitter([tanz], f, art: EmoteKind.Expression));
    }

    [Fact]
    public void Ausdruck_landet_nur_im_Ausdrucks_Tab()
    {
        var f = new Favourites();

        Assert.Empty(Gitter([Smile], f, art: EmoteKind.Emote));
        Assert.Empty(Gitter([Smile], f, art: EmoteKind.Dance));
        Assert.Equal([Smile], Gitter([Smile], f, art: EmoteKind.Expression));
        Assert.Empty(Gitter([Smile], Mit(69), art: EmoteKind.Expression));
    }

    [Fact]
    public void Suche_trifft_Namen_und_Befehl()
    {
        var f = new Favourites();

        Assert.Equal([Dote], Gitter([Pet, Dote], f, "dot"));
        Assert.Equal([Pet], Gitter([Pet, Dote], f, "stroke"));
    }

    [Fact]
    public void Suche_ist_Gross_und_Kleinschreibung_egal()
    {
        Assert.Equal([Pet], Gitter([Pet, Dote], new Favourites(), "PET"));
    }

    [Fact]
    public void Suche_trifft_auch_Mod_Namen()
    {
        var a = Emote(1, "Alpha", "/a", mod: "Glitzer Pack");
        var b = Emote(2, "Beta", "/b", mod: "Anderes");

        Assert.Equal([a], Gitter([a, b], new Favourites(), "glitzer"));
    }

    /// <summary>Die Reihe lebt von ihrem festen Platz, deshalb wirkt hier nichts ausser dem Praedikat.</summary>
    [Fact]
    public void Reihe_bleibt_vom_Gitter_Filter_unberuehrt()
    {
        var a = Emote(1, "Alpha", "/a", mod: "M1");

        Assert.Equal([a], ListBuilder.Pinned([a], Mit(1)));
        Assert.Empty(Gitter([a], Mit(1)));
    }

    [Fact]
    public void Leerer_Mod_Filter_laesst_alles_durch()
    {
        var a = Emote(1, "Alpha", "/a", mod: "M1");
        var b = Emote(2, "Beta", "/b", mod: "M2");

        Assert.Equal([a, b], Gitter([a, b], new Favourites(), filter: KeinFilter));
    }

    [Fact]
    public void Mod_Filter_ohne_Treffer_leert_das_Gitter()
    {
        var a = Emote(1, "Alpha", "/a", mod: "M1");

        Assert.Empty(Gitter([a], new Favourites(), filter: Filter("gibt es nicht")));
    }

    [Fact]
    public void Mehrere_Mods_im_Filter_sind_eine_Vereinigung()
    {
        var a = Emote(1, "Alpha", "/a", mod: "M1");
        var b = Emote(2, "Beta", "/b", mod: "M2");
        var c = Emote(3, "Gamma", "/c", mod: "M3");

        Assert.Equal([a, c], Gitter([a, b, c], new Favourites(), filter: Filter("M1", "M3")));
    }

    [Fact]
    public void Emote_mit_mehreren_Mods_trifft_ueber_jeden_davon()
    {
        var a = new ModdedEmote(Pet.Emote, ["M1", "M2"]);

        Assert.Equal([a], Gitter([a], new Favourites(), filter: Filter("M2")));
    }

    /// <summary>
    /// Der Fall aus der Spec: Ein Mod wird abgeschaltet, das Emote verschwindet damit
    /// aus der Liste. Der Favorit bleibt gespeichert - hier taucht er nur nicht auf.
    /// </summary>
    [Fact]
    public void Favorit_ohne_aktiven_Mod_faellt_aus_der_Reihe()
    {
        var favoriten = Mit(105, 146);

        Assert.Equal([Dote], ListBuilder.Pinned([Dote], favoriten));
        Assert.True(favoriten.Contains(105));
    }

    [Fact]
    public void Ausblende_Praedikat_wirkt_in_Reihe_und_Gitter()
    {
        bool Aus(ModdedEmote m) => m.Emote.Id == 105;

        Assert.Equal([Dote], ListBuilder.Pinned([Pet, Dote], Mit(105, 146), Aus));
        Assert.Equal([Hug], Gitter([Pet, Hug], new Favourites(), aus: Aus));
    }

    [Fact]
    public void Mod_Namen_sind_eindeutig_und_sortiert_je_Tab()
    {
        var a = Emote(1, "Alpha", "/a", mod: "Zeta");
        var b = Emote(2, "Beta", "/b", mod: "alpha");
        var c = Emote(3, "Gamma", "/c", mod: "Zeta");
        var t = Emote(4, "Tanz", "/t", tanz: true, mod: "Nur Tanz");

        var x = Emote(5, "Mimik", "/m", mod: "Gesicht", kategorie: Ausdruecke);

        Assert.Equal(["alpha", "Zeta"], ListBuilder.ModNames([a, b, c, t, x], EmoteKind.Emote));
        Assert.Equal(["Nur Tanz"], ListBuilder.ModNames([a, b, c, t, x], EmoteKind.Dance));
        Assert.Equal(["Gesicht"], ListBuilder.ModNames([a, b, c, t, x], EmoteKind.Expression));
    }

    [Fact]
    public void Leere_Liste_bleibt_leer()
    {
        Assert.Empty(ListBuilder.Pinned([], Mit(105)));
        Assert.Empty(Gitter([], Mit(105)));
        Assert.Empty(ListBuilder.ModNames([], EmoteKind.Emote));
    }
}
