using Dalamud.Configuration;

namespace EnMotion;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>
    /// Angeheftete Emotes, in der Reihenfolge des Hinzufuegens. Nur Emotes und
    /// Taenze - Posen haben keine Nummer in diesem Sinn, sondern eine Haltung und
    /// einen Index darin.
    /// </summary>
    public List<uint> Favourites { get; set; } = [];

    // --- der Knopf ------------------------------------------------------------

    public bool ButtonVisible { get; set; } = true;

    /// <summary>Gegen Verrutschen im Kampf.</summary>
    public bool ButtonLocked { get; set; }

    public float ButtonX { get; set; } = 300f;
    public float ButtonY { get; set; } = 300f;

    /// <summary>
    /// Symbol auf dem Knopf. 246267 ist das Symbol von „Pet" aus der Emote-Tabelle.
    /// </summary>
    public uint ButtonIconId { get; set; } = 246267;

    /// <summary>Kantenlaenge der Symbole im Gitter.</summary>
    public float IconSize { get; set; } = 48f;
}
