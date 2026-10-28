namespace EnMotion.Core;

/// <summary>
/// Ein Emote samt der Mods, die es anfassen. Ohne Mods ist es ein ungemoddetes Emote, das nur als Favorit
/// in die Anzeige kommt.
/// </summary>
public sealed record ModdedEmote(EmoteEntry Emote, IReadOnlyList<string> Mods)
{
    public bool IsModded => Mods.Count > 0;
}
