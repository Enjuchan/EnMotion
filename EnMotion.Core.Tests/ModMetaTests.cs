using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

/// <summary>Das Format FileVersion 4 aus Penumbra 1.7: Gruppen und Standarddateien stehen in meta.json.</summary>
public class ModMetaTests
{
    // Aufbau und Feldnamen wie in Eorzean Nightlife und anderen Mods aus E:\Mare Mods (Id je Gruppe und Option,
    // Priority nur wo gesetzt, Standarddateien in DefaultData).
    private const string Meta = """
        {
        	"FileVersion": 4,
        	"Identifier": "3b715087-2070-468b-99e1-7268218e36d0",
        	"Name": "Eorzean-Nightlife",
        	"Description": "Credit \u2665",
        	"Groups": [
        		{ "Type": "Single", "Id": "g0", "Name": "Default", "Options": [ { "Id": "o0", "Name": "Default" } ] },
        		{ "Type": "Multi", "Id": "g1", "Name": "~\u2665CuddleNsleep\u2665~", "Priority": 14, "Options": [
        			{ "Id": "o1", "Name": "064 Cuddle", "Priority": 3, "Files": {
        				"chara/human/c0101/animation/a0001/bt_common/emote/stroke_top.pap": "a\\b.pap" } },
        			{ "Id": "o2", "Name": "Swap", "FileSwaps": {
        				"chara/human/c0101/animation/a0001/bt_common/emote/goodbye_st.pap": "chara/x.pap" } } ] },
        		{ "Type": "Imc", "Id": "g2", "Name": "Imc", "Identifier": {}, "DefaultEntry": {}, "Options": [] }
        	],
        	"DefaultData": { "Files": { "chara/human/c0101/animation/a0001/bt_common/emote/dance_female_loop.pap": "d.pap" } }
        }
        """;

    [Fact]
    public void Gruppen_Optionen_und_Standarddateien_werden_gelesen()
    {
        var layout = ModLayoutReader.ParseMeta("\uFEFF" + Meta)!;

        Assert.Equal(["chara/human/c0101/animation/a0001/bt_common/emote/dance_female_loop.pap"], layout.DefaultPaths);
        Assert.Equal(["Default", "~\u2665CuddleNsleep\u2665~"], layout.Groups.Select(g => g.Name));

        var cuddle = layout.Groups[1];
        Assert.True(cuddle.Multi);
        Assert.Equal(14, cuddle.Priority);
        Assert.Equal(["064 Cuddle", "Swap"], cuddle.Options.Select(o => o.Name));
        Assert.Equal(3, cuddle.Options[0].Priority);
        Assert.Equal(["chara/human/c0101/animation/a0001/bt_common/emote/stroke_top.pap"], cuddle.Options[0].Paths);
        Assert.Equal(["chara/human/c0101/animation/a0001/bt_common/emote/goodbye_st.pap"], cuddle.Options[1].Paths);
        Assert.False(layout.Groups[0].Multi);
    }

    [Fact]
    public void Mod_ohne_Gruppen_hat_nur_Standarddateien()
    {
        const string json = """{ "FileVersion": 4, "Name": "Solo", "DefaultData": { "Files": { "A/B.pap": "x" } } }""";

        var layout = ModLayoutReader.ParseMeta(json)!;

        Assert.Empty(layout.Groups);
        Assert.Equal(["a/b.pap"], layout.DefaultPaths);
    }

    [Fact]
    public void Mod_ganz_ohne_Dateien_ergibt_ein_leeres_Layout()
    {
        var layout = ModLayoutReader.ParseMeta("""{ "FileVersion": 4, "Name": "Leer" }""")!;

        Assert.Empty(layout.Groups);
        Assert.Empty(layout.DefaultPaths);
    }

    [Theory]
    [InlineData("""{ "FileVersion": 3, "Name": "Alt", "Groups": [] }""")]
    [InlineData("""{ "Name": "Ohne Version" }""")]
    public void Anderes_Format_ergibt_null_und_der_Rueckfall_greift(string json)
    {
        Assert.Null(ModLayoutReader.ParseMeta(json));
    }

    [Fact]
    public void Varianten_kommen_aus_dem_Format_4()
    {
        var mod = new ModRef("pack", "Pack");
        var layout = ModLayoutReader.ParseMeta(Meta)!;

        IEnumerable<string> Names(string key) => key switch
        {
            "emote/stroke_top" => ["Pet"],
            "emote/goodbye_st" => ["Wave"],
            "emote/dance_female_loop" => ["Harvest Dance"],
            _ => [],
        };

        var index = ModIndex.Build([(mod, ["Emote: Pet", "Emote: Wave", "Emote: Harvest Dance"])], _ => layout, Names);

        Assert.Equal(new Variant(mod, "~\u2665CuddleNsleep\u2665~", "064 Cuddle"), Assert.Single(index.VariantsOf("Pet")).Variant);
        Assert.Equal(new Variant(mod, "~\u2665CuddleNsleep\u2665~", "Swap"), Assert.Single(index.VariantsOf("Wave")).Variant);
        Assert.Equal(new Variant(mod), Assert.Single(index.VariantsOf("Harvest Dance")).Variant);
    }
}
