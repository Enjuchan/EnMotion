namespace EnMotion.Core;

/// <summary>Eingeschaltet-Zustand und Prioritaet einer Mod in der Sammlung, wie Penumbra sie meldet.</summary>
public readonly record struct ModSetting(string Mod, bool Enabled, int Priority);

public static class ModPriority
{
    /// <summary>
    /// Die Prioritaet, mit der <paramref name="chosen"/> alle uebrigen eingeschalteten Mods eines Emotes
    /// uebertrumpft: eins ueber der hoechsten von ihnen, hoechstens aber nie niedriger als ihre bisherige,
    /// weil eine Wahl die Mod nicht schwaecher machen soll. Ohne andere eingeschaltete Mods bleibt ihre
    /// bisherige Prioritaet (ohne Eintrag 0). Abgeschaltete Mods zaehlen nicht, sie wirken nicht mit.
    /// </summary>
    public static int Target(string chosen, IEnumerable<ModSetting> settings)
    {
        var own = 0;
        int? othersMax = null;

        foreach (var s in settings)
        {
            if (s.Mod == chosen)
                own = s.Priority;
            else if (s.Enabled)
                othersMax = othersMax is { } m ? Math.Max(m, s.Priority) : s.Priority;
        }

        return othersMax is { } max ? Math.Max(own, max + 1) : own;
    }
}
