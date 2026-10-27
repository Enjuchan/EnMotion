using Dalamud.Configuration;
using EnMotion.Core;

namespace EnMotion;

public enum ViewMode { Full, Compact }

public enum ButtonStyle { Hotbar, EnMotion }

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

    /// <summary>Zuletzt gewaehlter Reiter im Hauptfenster.</summary>
    public int LastTab { get; set; }

    /// <summary>Angeheftete Posen, in der Reihenfolge des Anheftens.</summary>
    public List<PoseFavouriteEntry> PoseFavourites { get; set; } = [];

    // --- das Fenster ----------------------------------------------------------

    public ViewMode View { get; set; } = ViewMode.Full;

    /// <summary>Fenstergroesse je Ansicht in Pixeln, gemerkt beim Verlassen; 0 heisst, es gibt noch keine.</summary>
    public float FullWidth { get; set; }
    public float FullHeight { get; set; }
    public float CompactWidth { get; set; }
    public float CompactHeight { get; set; }

    /// <summary>Deckkraft des Fensterhintergrunds, 1.0 ist deckend.</summary>
    public float BackgroundOpacity { get; set; } = 1f;

    public bool WindowLocked { get; set; }
    public bool HideInCombat { get; set; }
    public bool CloseAfterPlay { get; set; }
    public bool ShowNames { get; set; }
    public bool HideLockedEmotes { get; set; }

    public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Hotbar;
}
