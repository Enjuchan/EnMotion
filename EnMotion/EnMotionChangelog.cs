namespace EnMotion;

/// <summary>Alle Versionen mit Datum des Release-Tags, neueste zuerst.</summary>
public static class EnMotionChangelog
{
    public static readonly IReadOnlyList<ChangelogEntry> Entries =
    [
        new("0.2.3.0", "2026-10-28",
        [
            new("A welcome window appears after you install EnMotion, with the first steps and the key settings.",
                "Nach der Installation erscheint ein Willkommensfenster mit den ersten Schritten und den wichtigsten Einstellungen."),
            new("A changelog window appears after every update and lists all versions; new ones are marked.",
                "Nach jedem Update erscheint ein Changelog-Fenster mit allen Versionen; neue sind markiert."),
            new("Both windows can be opened again from the About tab.",
                "Beide Fenster lassen sich im About-Tab erneut öffnen."),
            new("The language switch moved from the title bar into the header band, away from the window buttons.",
                "Der Sprachschalter sitzt jetzt im Kopfband statt in der Titelleiste, weg von den Fensterknöpfen."),
        ]),
        new("0.2.2.0", "2026-10-28",
        [
            new("EnMotion is available in German and English. A language button in the title bar switches instantly.",
                "EnMotion gibt es auf Deutsch und Englisch. Ein Sprach-Knopf in der Titelleiste wechselt sofort."),
            new("It starts in Dalamud's language until you choose one yourself.",
                "Es startet in der Sprache von Dalamud, bis du selbst eine wählst."),
            new("The tab bar switches to icons when the window is too narrow.",
                "Die Tab-Leiste schaltet auf Icons um, wenn das Fenster zu schmal ist."),
        ]),
        new("0.2.1.0", "2026-10-28",
        [
            new("New Dependencies tab with a Penumbra status card and an install button.",
                "Neuer Dependencies-Tab mit Statuskarte für Penumbra und Install-Knopf."),
            new("New About tab with description, commands and license.",
                "Neuer About-Tab mit Beschreibung, Befehlen und Lizenz."),
            new("New Diagnostics switch in Settings shows the draw time of the window.",
                "Neuer Diagnose-Schalter in den Settings zeigt die Zeichenzeit des Fensters."),
            new("New slider for the size of the button.",
                "Neuer Regler für die Größe des Knopfs."),
            new("Nothing is drawn during loading screens. The settings button in the plugin installer opens the full view on Settings.",
                "In Ladebildschirmen wird nichts gezeichnet. Der Einstellungs-Knopf im Plugin-Installer öffnet die Vollansicht auf Settings."),
        ]),
        new("0.2.0.1", "2026-10-28",
        [
            new("Window and button no longer appear on the title screen; they show up after you log in.",
                "Fenster und Knopf erscheinen nicht mehr auf dem Titelbildschirm, sondern erst nach dem Login."),
        ]),
        new("0.2.0.0", "2026-10-28",
        [
            new("New look shared with the other En plugins, with tabs for Emotes, Dances, Expressions, Poses and Settings.",
                "Neuer Look wie bei den anderen En-Plugins, mit Tabs für Emotes, Dances, Expressions, Poses und Settings."),
            new("Favourites sit in a row at the top, marked with a heart, emotes and poses together.",
                "Favoriten stehen oben in einer Reihe, mit Herz markiert, Emotes und Posen zusammen."),
            new("A mod filter shows only what a chosen mod changes; the search also matches mod names.",
                "Ein Mod-Filter zeigt nur, was ein gewählter Mod verändert; die Suche findet auch Mod-Namen."),
            new("New compact view, background opacity, locking, hiding in combat and closing after playing.",
                "Neu sind die Kompaktansicht, Hintergrund-Deckkraft, Fixieren, Ausblenden im Kampf und Schließen nach dem Abspielen."),
            new("The button comes in two styles, with a choosable icon.",
                "Der Knopf gibt es in zwei Stilen, mit frei wählbarem Icon."),
            new("New plugin icon.",
                "Neues Plugin-Icon."),
        ]),
        new("0.1.0.0", "2026-08-30",
        [
            new("First version: lists the emotes, dances and idle poses that your enabled Penumbra mods change and plays them with one click.",
                "Erste Version: listet die Emotes, Tänze und Idle-Posen, die deine aktiven Penumbra-Mods verändern, und spielt sie mit einem Klick ab."),
            new("Pick idle poses directly instead of cycling through them.",
                "Idle-Posen wählst du direkt, statt sie durchzuschalten."),
            new("A small button on screen opens the window; you can drag it anywhere.",
                "Ein kleiner Knopf auf dem Bildschirm öffnet das Fenster; du kannst ihn frei verschieben."),
            new("Favourites for quick access.",
                "Favoriten für den schnellen Zugriff."),
        ]),
    ];
}
