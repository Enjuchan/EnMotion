using Dalamud.Interface;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>Angaben für den About-Tab; Wirkung des Befehls steht auch als Hilfetext im Befehl selbst.</summary>
public static class EnMotionAbout
{
    public const string ToggleEffect = "Opens or closes the EnMotion window.";

    public static AboutInfo Create(string version) => new(
        "EnMotion",
        version,
        "Lists emotes, dances, expressions and idle poses altered by Penumbra mods and plays them with one click.",
        [new("/enmotion", ToggleEffect)],
        "AGPL-3.0",
        "EnMotion - Copyright (c) 2026 Enjuchan",
        [new("Open GitHub", FontAwesomeIcon.ExternalLinkAlt, "https://github.com/Enjuchan/EnMotion")]);
}
