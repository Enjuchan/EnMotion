using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class ListBuilderTests
{
    private static ModdedEmote Emote(uint id, string name, string command, bool tanz = false) =>
        new(new EmoteEntry(id, name, 0, command, [tanz ? "emote/dance01" : "emote/norm01"]),
            ["irgendein Mod"]);

    private static readonly ModdedEmote Pet = Emote(105, "Pet", "/stroke");
    private static readonly ModdedEmote Dote = Emote(146, "Dote", "/dote");
    private static readonly ModdedEmote Hug = Emote(112, "Hug", "/hug");
    private static readonly ModdedEmote Dance = Emote(20, "Ballroom", "/ballroom", tanz: true);

    private static Favourites Mit(params uint[] ids)
    {
        var f = new Favourites();
        f.Load(ids);
        return f;
    }

    [Fact]
    public void Favoriten_stehen_oben_der_Rest_darunter()
    {
        var (pinned, rest) = ListBuilder.Build([Pet, Dote, Hug], Mit(146), "", taenze: false);

        Assert.Equal([Dote], pinned);
        Assert.Equal([Pet, Hug], rest);
    }

    /// <summary>
    /// Die angeheftete Reihe folgt der Favoritenliste, nicht der Emote-Liste - sonst
    /// waere die gewaehlte Reihenfolge wirkungslos.
    /// </summary>
    [Fact]
    public void Angeheftete_folgen_der_Favoritenreihenfolge()
    {
        var (pinned, _) = ListBuilder.Build([Pet, Dote, Hug], Mit(112, 105), "", taenze: false);

        Assert.Equal([Hug, Pet], pinned);
    }

    [Fact]
    public void Taenze_und_Emotes_werden_getrennt()
    {
        var (_, restEmotes) = ListBuilder.Build([Pet, Dance], new Favourites(), "", taenze: false);
        var (_, restTaenze) = ListBuilder.Build([Pet, Dance], new Favourites(), "", taenze: true);

        Assert.Equal([Pet], restEmotes);
        Assert.Equal([Dance], restTaenze);
    }

    [Fact]
    public void Suche_trifft_Namen_und_Befehl()
    {
        var (_, nachName) = ListBuilder.Build([Pet, Dote], new Favourites(), "dot", taenze: false);
        var (_, nachBefehl) = ListBuilder.Build([Pet, Dote], new Favourites(), "stroke", taenze: false);

        Assert.Equal([Dote], nachName);
        Assert.Equal([Pet], nachBefehl);
    }

    [Fact]
    public void Suche_ist_Gross_und_Kleinschreibung_egal()
    {
        var (_, rest) = ListBuilder.Build([Pet, Dote], new Favourites(), "PET", taenze: false);

        Assert.Equal([Pet], rest);
    }

    /// <summary>Die Suche gilt auch fuer die angeheftete Reihe.</summary>
    [Fact]
    public void Suche_filtert_auch_die_Favoriten()
    {
        var (pinned, _) = ListBuilder.Build([Pet, Dote], Mit(105, 146), "dot", taenze: false);

        Assert.Equal([Dote], pinned);
    }

    /// <summary>
    /// Der Fall aus der Spec: Ein Mod wird abgeschaltet, das Emote verschwindet damit
    /// aus der Liste. Der Favorit bleibt gespeichert - hier taucht er nur nicht auf.
    /// </summary>
    [Fact]
    public void Favorit_ohne_aktives_Emote_taucht_nicht_auf()
    {
        var favoriten = Mit(105, 146);

        var (pinned, _) = ListBuilder.Build([Dote], favoriten, "", taenze: false);

        Assert.Equal([Dote], pinned);
        Assert.True(favoriten.Contains(105));
    }

    [Fact]
    public void Leere_Liste_bleibt_leer()
    {
        var (pinned, rest) = ListBuilder.Build([], Mit(105), "", taenze: false);

        Assert.Empty(pinned);
        Assert.Empty(rest);
    }
}
