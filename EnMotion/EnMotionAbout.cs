using Dalamud.Interface;
using EnMotion.Ui;

namespace EnMotion;

/// <summary>Angaben für den About-Tab; Wirkung des Befehls steht auch als Hilfetext im Befehl selbst.</summary>
public static class EnMotionAbout
{
    public static readonly LocText ToggleEffect = new("Opens or closes the EnMotion window.", "Öffnet oder schließt das EnMotion-Fenster.");

    public static AboutInfo Create(string version) => new(
        "EnMotion",
        version,
        new("Lists emotes, dances, expressions and idle poses altered by Penumbra mods and plays them with one click.",
            "Listet Emotes, Tänze, Mimik und Idle-Posen, die Penumbra-Mods verändern, und spielt sie mit einem Klick ab."),
        [new("/enmotion", ToggleEffect)],
        "AGPL-3.0",
        "EnMotion - Copyright (c) 2026 Enjuchan",
        [new(new("Open GitHub", "GitHub öffnen"), FontAwesomeIcon.ExternalLinkAlt, "https://github.com/Enjuchan/EnMotion")]);
}
