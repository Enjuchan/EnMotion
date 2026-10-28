namespace EnMotion.Core;

/// <summary>
/// Ein Emote samt der installierten Mods, die es anfassen. Ohne Mods ist es ein ungemoddetes Emote, das nur
/// als Favorit in die Anzeige kommt. <paramref name="NoneActive"/> heisst: Mods sind installiert, aber keine
/// davon wirkt laut Sammlung gerade.
/// </summary>
public sealed record ModdedEmote(EmoteEntry Emote, IReadOnlyList<string> Mods, bool NoneActive = false)
{
    public bool IsModded => Mods.Count > 0;

    /// <summary>Ob gerade eine Mod fuer dieses Emote wirkt; nur dann zeigt die Kachel den Mod-Rahmen.</summary>
    public bool HasActiveMod => IsModded && !NoneActive;
}
