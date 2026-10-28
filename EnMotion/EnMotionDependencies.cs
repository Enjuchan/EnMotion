namespace EnMotion;

/// <summary>EnMotions Abhängigkeiten für den Dependencies-Tab.</summary>
public static class EnMotionDependencies
{
    public static readonly IReadOnlyList<DependencyGroup> Groups =
    [
        new("Required", DependencyKind.Required,
        [
            // Penumbra liegt nicht in Dalamuds Haupt-Repository (am 2026-10-28 in der Installation geprüft:
            // IsThirdParty true, InstalledFromUrl zeigt auf Ottermandias' SeaOfStars).
            new("Penumbra", "Penumbra", "Detects which emotes your mods change.", "https://github.com/xivdev/Penumbra",
                ["https://raw.githubusercontent.com/Ottermandias/SeaOfStars/main/repo.json"]),
        ]),
    ];
}
