using EnMotion.Core;
using Xunit;

namespace EnMotion.Core.Tests;

public class ModPriorityTests
{
    [Fact]
    public void Eins_ueber_der_hoechsten_anderen_eingeschalteten_Mod()
    {
        var target = ModPriority.Target("B", [new("A", true, 3), new("C", true, 7), new("B", false, 1)]);

        Assert.Equal(8, target);
    }

    [Fact]
    public void Ohne_andere_eingeschaltete_Mods_bleibt_die_eigene_Prioritaet()
    {
        Assert.Equal(4, ModPriority.Target("B", [new("B", false, 4), new("A", false, 99)]));
        Assert.Equal(4, ModPriority.Target("B", [new("B", true, 4)]));
    }

    [Fact]
    public void Ohne_Eintraege_ist_das_Ergebnis_null()
    {
        Assert.Equal(0, ModPriority.Target("B", []));
    }

    [Fact]
    public void Hat_die_gewaehlte_Mod_schon_die_hoechste_Prioritaet_wird_sie_nicht_gesenkt()
    {
        Assert.Equal(9, ModPriority.Target("B", [new("B", true, 9), new("A", true, 5)]));
    }

    [Fact]
    public void Gleiche_Prioritaeten_werden_um_eins_uebertroffen()
    {
        Assert.Equal(6, ModPriority.Target("B", [new("B", true, 5), new("A", true, 5)]));
    }

    [Fact]
    public void Abgeschaltete_Mods_zaehlen_nicht()
    {
        Assert.Equal(3, ModPriority.Target("B", [new("B", true, 2), new("A", true, 2), new("C", false, 50)]));
    }

    [Fact]
    public void Negative_Prioritaeten_werden_richtig_behandelt()
    {
        Assert.Equal(-1, ModPriority.Target("B", [new("B", false, -5), new("A", true, -2)]));
    }
}
