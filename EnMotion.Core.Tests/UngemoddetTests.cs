using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class UngemoddetTests
{
    private static EmoteEntry E(uint id, string name, string command, bool tanz = false) =>
        new(id, name, 0, command, [tanz ? "emote/dance01" : "emote/norm01"], 1);

    private static readonly EmoteEntry Pet = E(105, "Pet", "/stroke");
    private static readonly EmoteEntry Wave = E(16, "Wave", "/wave");
    private static readonly EmoteEntry Ballroom = E(20, "Ballroom Dance", "/ballroom", tanz: true);

    private static Favourites Mit(params uint[] ids)
    {
        var f = new Favourites();
        f.Load(ids);
        return f;
    }

    [Fact]
    public void Gemoddet_gilt_nur_mit_Mods()
    {
        Assert.True(new ModdedEmote(Pet, ["Mod A"]).IsModded);
        Assert.False(new ModdedEmote(Pet, []).IsModded);
    }

    [Fact]
    public void Ungemoddeter_Favorit_bleibt_in_der_Reihe_an_seinem_Platz()
    {
        var gemoddet = new[] { new ModdedEmote(Pet, ["Mod A"]) };
        var pinned = ListBuilder.Pinned(gemoddet, Mit(16, 105), alleEmotes: [Pet, Wave]);

        Assert.Equal([16u, 105u], pinned.Select(m => m.Emote.Id));
        Assert.False(pinned[0].IsModded);
        Assert.True(pinned[1].IsModded);
    }

    [Fact]
    public void Abgeschalteter_Mod_laesst_den_Favoriten_als_ungemoddet_stehen()
    {
        var pinned = ListBuilder.Pinned([], Mit(105), alleEmotes: [Pet]);

        Assert.Single(pinned);
        Assert.False(pinned[0].IsModded);
    }

    [Fact]
    public void Ohne_Katalog_faellt_der_Ungemoddete_wie_frueher_heraus()
    {
        Assert.Empty(ListBuilder.Pinned([], Mit(105)));
    }

    [Fact]
    public void Ausblenden_wirkt_auch_auf_Ungemoddete()
    {
        var pinned = ListBuilder.Pinned([], Mit(105, 16), m => m.Emote.Id == 16, [Pet, Wave]);

        Assert.Equal([105u], pinned.Select(m => m.Emote.Id));
    }

    [Fact]
    public void Unbekannte_Id_im_Katalog_wird_uebersprungen()
    {
        Assert.Empty(ListBuilder.Pinned([], Mit(999), alleEmotes: [Pet]));
    }

    [Fact]
    public void Hinzufuegen_haengt_ans_Ende_und_nie_doppelt()
    {
        var f = Mit(105);

        Assert.True(f.Add(16));
        Assert.False(f.Add(16));
        Assert.Equal([105u, 16u], f.Ids);
    }

    [Fact]
    public void Suche_findet_Teiltreffer_ohne_Gross_Klein_in_Name_und_Befehl()
    {
        var alle = new[] { Pet, Wave, Ballroom };

        Assert.Equal([105u], ListBuilder.AddCandidates(alle, Mit(), "PE", _ => true).Select(c => c.Emote.Id));
        Assert.Equal([16u], ListBuilder.AddCandidates(alle, Mit(), "/WAV", _ => true).Select(c => c.Emote.Id));
        Assert.Empty(ListBuilder.AddCandidates(alle, Mit(), "xyz", _ => true));
    }

    [Fact]
    public void Leere_Suche_liefert_alle_nach_Namen_sortiert_Taenze_zaehlen_mit()
    {
        var alle = new[] { Wave, Pet, Ballroom };

        Assert.Equal([20u, 105u, 16u], ListBuilder.AddCandidates(alle, Mit(), "  ", _ => true).Select(c => c.Emote.Id));
    }

    [Fact]
    public void Gesperrte_erscheinen_nicht()
    {
        var treffer = ListBuilder.AddCandidates([Pet, Wave], Mit(), "", e => e.Id != 16);

        Assert.Equal([105u], treffer.Select(c => c.Emote.Id));
    }

    [Fact]
    public void Schon_favorisierte_sind_markiert()
    {
        var treffer = ListBuilder.AddCandidates([Pet, Wave], Mit(105), "", _ => true);

        Assert.True(treffer.Single(c => c.Emote.Id == 105).Favourite);
        Assert.False(treffer.Single(c => c.Emote.Id == 16).Favourite);
    }

    // ---- Zug und Gleiten

    [Fact]
    public void Ease_beginnt_bei_0_endet_bei_1_und_ist_am_Anfang_schnell()
    {
        Assert.Equal(0f, Reorder.Ease(0f));
        Assert.Equal(1f, Reorder.Ease(Reorder.GlideSeconds));
        Assert.Equal(1f, Reorder.Ease(5f));
        Assert.Equal(0f, Reorder.Ease(-1f));
        Assert.True(Reorder.Ease(Reorder.GlideSeconds / 2f) > 0.5f);
    }

    [Fact]
    public void Reihenfolge_im_Zug_oeffnet_die_Luecke()
    {
        Assert.Equal([1, 2, 0, 3], Reorder.OrderDuringDrag(4, 0, 2));
        Assert.Equal([3, 0, 1, 2], Reorder.OrderDuringDrag(4, 3, 0));
        Assert.Equal([0, 1, 2], Reorder.OrderDuringDrag(3, 1, 1));
        Assert.Equal([0, 1, 2], Reorder.OrderDuringDrag(3, 5, 1));
    }

    [Fact]
    public void Zielplaetze_gleicher_Breite_in_einer_Zeile()
    {
        var p = Reorder.Positions([10f, 10f, 10f], 0, 2, 0f, 0f, 10f, 2f, 2f, 100f);

        Assert.Equal((24f, 0f), p[0]);
        Assert.Equal((0f, 0f), p[1]);
        Assert.Equal((12f, 0f), p[2]);
    }

    [Fact]
    public void Zielplaetze_brechen_um_und_nehmen_unterschiedliche_Breiten_mit()
    {
        // Rechts bei 30: 10 + 2 + 10 passt, das dritte (10) nicht mehr.
        var p = Reorder.Positions([10f, 10f, 10f], 2, 0, 0f, 0f, 10f, 2f, 2f, 30f);

        Assert.Equal((0f, 0f), p[2]);
        Assert.Equal((12f, 0f), p[0]);
        Assert.Equal((0f, 12f), p[1]);

        var breit = Reorder.Positions([10f, 20f, 10f], 0, 1, 0f, 0f, 10f, 2f, 2f, 100f);
        Assert.Equal((22f, 0f), breit[0]);
        Assert.Equal((0f, 0f), breit[1]);
    }

    [Fact]
    public void Ohne_Zug_bleiben_die_Plaetze()
    {
        var p = Reorder.Positions([10f, 10f], -1, -1, 5f, 7f, 10f, 2f, 2f, 100f);

        Assert.Equal((5f, 7f), p[0]);
        Assert.Equal((17f, 7f), p[1]);
    }

    private static (float, float, float, float)[] Reihe3 =>
        [(0f, 0f, 10f, 10f), (12f, 0f, 22f, 10f), (24f, 0f, 34f, 10f)];

    [Theory]
    [InlineData(1f, 5f, 0, 0)]   // links vom ersten
    [InlineData(30f, 5f, 0, 2)]  // rechte Haelfte des letzten: ans Ende
    [InlineData(3f, 5f, 2, 0)]   // linke Haelfte des ersten
    [InlineData(40f, 5f, 0, 2)]  // hinter der Reihe: ans Ende
    [InlineData(17f, 5f, 1, 1)]  // auf sich selbst: unveraendert
    public void Drop_Index(float x, float y, int von, int erwartet) =>
        Assert.Equal(erwartet, Reorder.DropIndex(Reihe3, x, y, von));
}
