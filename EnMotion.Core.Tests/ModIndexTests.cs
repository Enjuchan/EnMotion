using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class ModIndexTests
{
    private static readonly ModRef A = new("dirA", "Mod A");
    private static readonly ModRef B = new("dirB", "Mod B");

    private static ModIndex Index() => ModIndex.Build(
    [
        (A, ["Emote: Wave", "Emote: Pet", "Item: Hat"]),
        (B, ["Emote: wave", "Emote: Wave"]),
    ]);

    [Fact]
    public void Emote_zu_Mods_ohne_Beachtung_der_Gross_und_Kleinschreibung()
    {
        Assert.Equal([A, B], Index().ModsOf("WAVE"));
        Assert.Equal([A], Index().ModsOf("Pet"));
    }

    [Fact]
    public void Mod_zu_Emotes_ohne_Doppelte_und_ohne_Gegenstaende()
    {
        Assert.Equal(["Wave", "Pet"], Index().EmotesOf("dirA"));
        Assert.Equal(["wave"], Index().EmotesOf("dirB"));
    }

    [Fact]
    public void Unbekanntes_liefert_leere_Listen()
    {
        Assert.Empty(Index().ModsOf("Dance"));
        Assert.Empty(Index().EmotesOf("nope"));
        Assert.Empty(ModIndex.Build([]).EmoteNames);
    }

    [Fact]
    public void Emote_ohne_Namen_wird_uebergangen()
    {
        var index = ModIndex.Build([(A, ["Emote: ", "Emote:   "])]);

        Assert.Empty(index.EmoteNames);
    }

    [Fact]
    public void Mod_ohne_Emotes_taucht_nicht_auf()
    {
        var index = ModIndex.Build([(A, ["Item: Hat"])]);

        Assert.Empty(index.EmoteNames);
        Assert.Empty(index.EmotesOf("dirA"));
    }
}
