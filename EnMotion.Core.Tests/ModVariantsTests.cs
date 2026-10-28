using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class ModVariantsTests
{
    private static readonly ModRef Pack = new("pack", "Eorzean Nightlife");
    private static readonly ModRef Simple = new("simple", "Simple Wave");

    // Pfadformen wie in Eorzean Nightlife: je Rasse/Geschlecht ein Skelettordner, dazu Nebendateien.
    private const string DanceLoop = "chara/human/c0101/animation/a0001/bt_common/emote/dance_female_loop.pap";
    private const string DanceLoopOther = "chara/human/c1101/animation/a0001/bt_common/emote/dance_female_loop.pap";
    private const string DanceStart = "chara/human/c0101/animation/a0001/bt_common/emote/dance_female_start.pap";
    private const string StrokeTop = "chara/human/c0101/animation/a0001/bt_common/emote/stroke_top.pap";
    private const string WaveFile = "chara/human/c0101/animation/a0001/bt_common/emote/goodbye_st.pap";

    private static IEnumerable<string> EmotesOfKey(string key) => key switch
    {
        "emote/dance_female_loop" or "emote/dance_female_start" => ["Harvest Dance"],
        "emote/stroke_top" => ["Pet"],
        "emote/goodbye_st" => ["Wave"],
        _ => [],
    };

    private const string GroupJson = """
        {
          "Version": 0, "Name": "Party-Pack", "Priority": 6, "Type": "Multi", "DefaultSettings": 0,
          "Options": [
            { "Name": "Click Flash", "Priority": 0, "Files": {
                "chara/human/c0101/animation/a0001/bt_common/emote/dance_female_loop.pap": "x.pap",
                "vfx/ocn/clickphone.avfx": "y.avfx" }, "FileSwaps": {}, "Manipulations": [] },
            { "Name": "Slow Dance", "Priority": 2, "Files": {
                "chara/human/c1101/animation/a0001/bt_common/emote/dance_female_loop.pap": "z.pap",
                "chara/human/c0101/animation/a0001/bt_common/emote/stroke_top.pap": "s.pap" } },
            { "Name": "Only Sound", "Priority": 0, "Files": { "sound/foo.scd": "f.scd" } }
          ]
        }
        """;

    private const string SingleJson = """
        { "Name": "Mode", "Priority": 1, "Type": "Single", "DefaultSettings": 0,
          "Options": [
            { "Name": "A", "Files": { "chara/human/c0101/animation/a0001/bt_common/emote/dance_female_start.pap": "a.pap" } },
            { "Name": "B", "Files": {} }
          ] }
        """;

    private const string DefaultJson = """
        { "Files": { "chara/human/c0101/animation/a0001/bt_common/emote/goodbye_st.pap": "w.pap" },
          "FileSwaps": {}, "Manipulations": [] }
        """;

    private static ModLayout PackLayout() => ModLayoutReader.Parse(null, [GroupJson, SingleJson]);

    private static ModIndex Index() => ModIndex.Build(
        [
            (Pack, ["Emote: Harvest Dance", "Emote: Pet", "Item: Hat"]),
            (Simple, ["Emote: Wave"]),
        ],
        mod => mod == Pack ? PackLayout() : ModLayoutReader.Parse(DefaultJson, []),
        EmotesOfKey);

    [Theory]
    [InlineData(DanceLoop, "emote/dance_female_loop")]
    [InlineData(DanceLoopOther, "emote/dance_female_loop")]
    [InlineData("CHARA\\human\\c0101\\animation\\a0001\\bt_common\\emote\\Dance_Female_Loop.pap", "emote/dance_female_loop")]
    [InlineData("chara/human/c0101/animation/a0001/bt_2km_emp/emote/pose01_loop.pap", "emote/pose01_loop")]
    [InlineData("chara/human/c0101/animation/a0001/bt_common/emote_sp/sp01_loop.pap", "emote_sp/sp01_loop")]
    public void Spielpfad_ergibt_den_ActionTimeline_Schluessel(string path, string key)
    {
        Assert.Equal(key, EmotePaths.KeyOf(path));
    }

    [Theory]
    [InlineData("vfx/ocn/clickphone.avfx")]
    [InlineData("common\\221\\dance_female_start.pap")]
    [InlineData("chara/human/c0101/animation/a0001/bt_common/emote/dance_female_loop.tmb")]
    public void Kein_Schluessel_fuer_andere_Dateien(string path)
    {
        Assert.Null(EmotePaths.KeyOf(path));
    }

    [Fact]
    public void Leser_kommt_mit_Bom_Sonderzeichen_und_Typen_zurecht()
    {
        const string group = "\uFEFF{ \"Name\": \"~\u2665cuddlensleep\u2665~\", \"Priority\": 3, \"Type\": \"Multi\", " +
            "\"Options\": [ { \"Name\": \"Zwei \\u2665\", \"Priority\": 1, \"Files\": { \"A\\\\B.PAP\": \"x\" } } ] }";
        const string imc = "{ \"Name\": \"Imc\", \"Type\": \"Imc\", \"Options\": [] }";

        var layout = ModLayoutReader.Parse("\uFEFF" + DefaultJson, [group, imc]);

        var parsed = Assert.Single(layout.Groups);
        Assert.Equal("~\u2665cuddlensleep\u2665~", parsed.Name);
        Assert.True(parsed.Multi);
        Assert.Equal(3, parsed.Priority);
        Assert.Equal(["a/b.pap"], parsed.Options[0].Paths);
        Assert.Single(layout.DefaultPaths);
    }

    [Fact]
    public void Optionen_werden_den_Emotes_ueber_ihre_Pfade_zugeordnet()
    {
        var index = Index();

        var dance = index.VariantsOf("Harvest Dance").Select(v => v.Variant).ToList();
        Assert.Equal(
            [new Variant(Pack, "Party-Pack", "Click Flash"), new Variant(Pack, "Party-Pack", "Slow Dance"), new Variant(Pack, "Mode", "A")],
            dance);

        Assert.Equal([new Variant(Pack, "Party-Pack", "Slow Dance")], index.VariantsOf("Pet").Select(v => v.Variant));
    }

    [Fact]
    public void Standarddateien_bleiben_ein_Eintrag_fuer_die_Mod()
    {
        var wave = Index().VariantsOf("Wave");

        Assert.Equal(new Variant(Simple), Assert.Single(wave).Variant);
    }

    [Fact]
    public void Ohne_Dateiwissen_oder_ohne_Treffer_bleibt_die_Mod_als_Ganzes()
    {
        var ohneDateien = ModIndex.Build([(Pack, ["Emote: Pet"])]);
        Assert.Equal(new Variant(Pack), Assert.Single(ohneDateien.VariantsOf("Pet")).Variant);

        var ohneTreffer = ModIndex.Build([(Simple, ["Emote: Wave"])], _ => PackLayout(), EmotesOfKey);
        Assert.Equal(new Variant(Simple), Assert.Single(ohneTreffer.VariantsOf("Wave")).Variant);
    }

    [Fact]
    public void Prioritaeten_der_Gruppe_und_Option_stehen_im_Eintrag()
    {
        var slow = Index().VariantsOf("Pet").Single();

        Assert.Equal(6, slow.GroupPriority);
        Assert.Equal(2, slow.OptionPriority);
    }

    [Fact]
    public void Weitere_Emotes_einer_Option()
    {
        var index = Index();

        Assert.Equal(["Harvest Dance", "Pet"], index.EmotesOf(new Variant(Pack, "Party-Pack", "Slow Dance")));
        Assert.Equal(["Harvest Dance"], index.EmotesOf(new Variant(Pack, "Party-Pack", "Click Flash")));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Options(params (string Group, string[] Selected)[] groups) =>
        groups.ToDictionary(g => g.Group, g => (IReadOnlyList<string>)g.Selected);

    [Fact]
    public void Multi_Wahl_schaltet_die_Option_ein_und_andere_Optionen_desselben_Emotes_aus()
    {
        var index = Index();
        var current = Options(("Party-Pack", ["Slow Dance", "Anderes"]), ("Mode", ["B"]));

        var result = VariantSettings.Apply(
            index.LayoutOf("pack"),
            current,
            [("Harvest Dance", new Variant(Pack, "Party-Pack", "Click Flash"))],
            emote => index.OptionsOf("pack", emote));

        Assert.Equal(["Anderes", "Click Flash"], result["Party-Pack"]);
        Assert.Equal(["B"], result["Mode"]);
    }

    [Fact]
    public void Single_Wahl_wird_die_Auswahl_der_Gruppe()
    {
        var index = Index();
        var current = Options(("Party-Pack", []), ("Mode", ["B"]));

        var result = VariantSettings.Apply(
            index.LayoutOf("pack"),
            current,
            [("Harvest Dance", new Variant(Pack, "Mode", "A"))],
            emote => index.OptionsOf("pack", emote));

        Assert.Equal(["A"], result["Mode"]);
        Assert.Empty(result["Party-Pack"]);
    }

    [Fact]
    public void Wahl_der_Mod_als_Ganzes_schaltet_nur_Multi_Optionen_desselben_Emotes_aus()
    {
        var index = Index();
        var current = Options(("Party-Pack", ["Click Flash", "Anderes"]), ("Mode", ["A"]));

        var result = VariantSettings.Apply(
            index.LayoutOf("pack"),
            current,
            [("Harvest Dance", new Variant(Pack))],
            emote => index.OptionsOf("pack", emote));

        Assert.Equal(["Anderes"], result["Party-Pack"]);
        Assert.Equal(["A"], result["Mode"]);
    }

    [Fact]
    public void Wahlen_mehrerer_Emotes_einer_Mod_werden_zusammengefuehrt()
    {
        var index = Index();
        var current = Options(("Party-Pack", []), ("Mode", ["B"]));

        var result = VariantSettings.Apply(
            index.LayoutOf("pack"),
            current,
            [
                ("Harvest Dance", new Variant(Pack, "Party-Pack", "Click Flash")),
                ("Pet", new Variant(Pack, "Party-Pack", "Slow Dance")),
            ],
            emote => index.OptionsOf("pack", emote));

        // Slow Dance aendert auch den Tanz, Click Flash aber nicht Pet: Beide Wahlen bleiben bestehen.
        Assert.Equal(["Click Flash", "Slow Dance"], result["Party-Pack"]);
    }

    [Fact]
    public void Apply_veraendert_die_Eingabe_nicht()
    {
        var index = Index();
        var current = Options(("Party-Pack", ["Slow Dance"]));

        VariantSettings.Apply(
            index.LayoutOf("pack"),
            current,
            [("Harvest Dance", new Variant(Pack, "Party-Pack", "Click Flash"))],
            emote => index.OptionsOf("pack", emote));

        Assert.Equal(["Slow Dance"], current["Party-Pack"]);
    }

    private static ModState State(bool enabled, int priority, params (string Group, string[] Selected)[] groups) =>
        new(enabled, priority, Options(groups));

    [Fact]
    public void Wirkende_Variante_ist_die_gewaehlte_Option_mit_hoechster_Gruppen_und_Optionspriorität()
    {
        var index = Index();
        var states = new Dictionary<ModRef, ModState> { [Pack] = State(true, 0, ("Party-Pack", ["Click Flash", "Slow Dance"]), ("Mode", ["A"])) };

        var effective = VariantSettings.Effective(index.VariantsOf("Harvest Dance"), m => states.GetValueOrDefault(m));

        // Gruppe Party-Pack (6) schlaegt Mode (1); darin gewinnt Slow Dance (2) gegen Click Flash (0).
        Assert.Equal(new Variant(Pack, "Party-Pack", "Slow Dance"), effective);
    }

    [Fact]
    public void Gleichstand_der_Optionen_nimmt_die_zuerst_genannte()
    {
        var index = ModIndex.Build(
            [(Pack, ["Emote: Harvest Dance"])],
            _ => ModLayoutReader.Parse(null, [GroupJson.Replace("\"Priority\": 2", "\"Priority\": 0")]),
            EmotesOfKey);
        var states = new Dictionary<ModRef, ModState> { [Pack] = State(true, 0, ("Party-Pack", ["Slow Dance", "Click Flash"])) };

        var effective = VariantSettings.Effective(index.VariantsOf("Harvest Dance"), m => states.GetValueOrDefault(m));

        Assert.Equal(new Variant(Pack, "Party-Pack", "Click Flash"), effective);
    }

    [Fact]
    public void Ohne_gewaehlte_Option_wirken_die_Standarddateien_oder_nichts()
    {
        var index = ModIndex.Build(
            [(Pack, ["Emote: Wave", "Emote: Pet"])],
            _ => ModLayoutReader.Parse(DefaultJson, [GroupJson]),
            EmotesOfKey);
        var states = new Dictionary<ModRef, ModState> { [Pack] = State(true, 0, ("Party-Pack", [])) };

        Assert.Equal(new Variant(Pack), VariantSettings.Effective(index.VariantsOf("Wave"), m => states.GetValueOrDefault(m)));
        Assert.Null(VariantSettings.Effective(index.VariantsOf("Pet"), m => states.GetValueOrDefault(m)));
    }

    [Fact]
    public void Mod_Prioritaet_entscheidet_zwischen_Mods_und_abgeschaltete_zaehlen_nicht()
    {
        var a = new ModRef("a", "A");
        var b = new ModRef("b", "B");
        var index = ModIndex.Build([(a, ["Emote: Wave"]), (b, ["Emote: Wave"])]);
        var states = new Dictionary<ModRef, ModState> { [a] = State(true, 5), [b] = State(true, 5) };

        Assert.Equal(new Variant(a), VariantSettings.Effective(index.VariantsOf("Wave"), m => states.GetValueOrDefault(m)));

        states[b] = State(true, 6);
        Assert.Equal(new Variant(b), VariantSettings.Effective(index.VariantsOf("Wave"), m => states.GetValueOrDefault(m)));

        states[b] = State(false, 99);
        Assert.Equal(new Variant(a), VariantSettings.Effective(index.VariantsOf("Wave"), m => states.GetValueOrDefault(m)));

        states[a] = State(false, 5);
        Assert.Null(VariantSettings.Effective(index.VariantsOf("Wave"), m => states.GetValueOrDefault(m)));
    }
}
