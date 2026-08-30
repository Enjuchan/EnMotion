namespace EnMotion.Core;

/// <summary>Ein veraendertes Emote samt der Mods, die es anfassen.</summary>
public sealed record ModdedEmote(EmoteEntry Emote, IReadOnlyList<string> Mods);
