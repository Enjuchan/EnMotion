using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace EnMotion;

/// <summary>Welche Pose gerade gilt und wie viele es davon gibt.</summary>
public readonly record struct PoseState(
    EmoteController.PoseType Type,
    byte Current,
    byte Count);

/// <summary>
/// Liest die Haltung des eigenen Charakters und schaltet einzelne Posen an.
/// </summary>
public static class PoseController
{
    /// <summary>
    /// Die zuletzt gueltige Haltung. Waehrend eines Emotes ist die aktuelle nicht
    /// ablesbar, sie aendert sich dadurch aber nicht - nach dem Emote steht der
    /// Charakter wieder so da wie vorher.
    /// </summary>
    private static EmoteController.PoseType lastValidType = EmoteController.PoseType.Idle;

    /// <summary>
    /// Haltung und Pose des eigenen Charakters, oder null wenn kein Charakter da ist -
    /// etwa im Ladebildschirm oder in der Charakterauswahl.
    /// </summary>
    public static unsafe PoseState? Read()
    {
        var player = Control.GetLocalPlayer();

        if (player is null)
            return null;

        var controller = &player->Character.EmoteController;
        var type = controller->CurrentPoseType;

        // Waehrend eines laufenden Emotes steht hier 0xFF - kein gueltiger Typ.
        // GetAvailablePoses beantwortet das nicht mit einem Fehler, sondern mit
        // Unsinn: im Spiel waren es 56 angebliche Posen. Statt den Reiter dann leer
        // zu lassen, gilt die Haltung von vorher weiter - so laesst sich aus dem
        // Emote heraus direkt in eine Pose wechseln.
        if (Enum.IsDefined(type))
            lastValidType = type;
        else
            type = lastValidType;

        return new PoseState(type, controller->CPoseState, PoseCount(type));
    }

    /// <summary>
    /// Wie viele Posen es in dieser Haltung gibt.
    ///
    /// GetAvailablePoses ist statisch - die Zahl haengt an der Haltung, nicht am
    /// Charakter - und liefert den **hoechsten gueltigen Index**, nicht die Anzahl.
    /// Beim Stehen gibt sie 6 zurueck, der Ring laeuft aber ueber sieben Zustaende
    /// 0 bis 6. Wer die Zahl als Anzahl nimmt, zeigt eine Pose zu wenig an und
    /// verrechnet sich bei jedem Sprung, der ueber den Ueberlauf geht, um genau
    /// einen Schritt.
    /// </summary>
    private static byte PoseCount(EmoteController.PoseType type) =>
        (byte)(EmoteController.GetAvailablePoses(type) + 1);

    /// <summary>
    /// Ob gerade ein Emote laeuft. Erklaert dem Spieler, warum keine Posen dastehen.
    /// </summary>
    public static unsafe bool IsEmoting()
    {
        var player = Control.GetLocalPlayer();

        return player is not null && player->Character.EmoteController.IsEmoting();
    }

    /// <summary>
    /// Ob die Haltung gerade ablesbar ist. Waehrend eines Emotes ist sie es nicht -
    /// dann steht in CurrentPoseType 0xFF und in CPoseState nicht die echte Pose.
    /// </summary>
    public static unsafe bool HasValidPose()
    {
        var player = Control.GetLocalPlayer();

        return player is not null && Enum.IsDefined(player->Character.EmoteController.CurrentPoseType);
    }

    /// <summary>Emote 90, "Pose aendern" - das, was hinter /cpose steckt.</summary>
    private const ushort ChangePoseEmote = 90;

    /// <summary>
    /// Auf eine bestimmte Pose der aktuellen Haltung wechseln.
    ///
    /// FFXIV kennt keinen Befehl, der eine Pose direkt setzt - "Pose aendern" rueckt
    /// immer nur um eine weiter. Den Zaehler vorzustellen half nicht: Das Schreiben
    /// von CPoseState kommt zwar an, aber die Aktion nimmt ihren Ausgangswert
    /// woanders her und ueberschreibt das Feld sofort wieder. CPoseState ist die
    /// Ablage des Ergebnisses, nicht der Eingang.
    ///
    /// Also alle noetigen Schritte auf einmal - im selben Frame. Dazwischen wird kein
    /// Bild gezeichnet, das Durchblaettern bleibt also unsichtbar.
    /// </summary>
    public static unsafe void Request(EmoteController.PoseType type, byte index)
    {
        // Laeuft noch ein Emote, laesst sich nicht rechnen: Die Haltung ist nicht
        // ablesbar und CPoseState gehoert noch zum alten Zustand. Also erst
        // abbrechen - "Pose aendern" beendet ein Emote, so wie im Spiel auch - und
        // den Wunsch aufheben, bis der Charakter wieder normal dasteht.
        if (!HasValidPose())
        {
            pending = (type, index);
            pendingUntil = DateTime.UtcNow + PendingTimeout;

            EmoteManager.Instance()->ExecuteEmote(ChangePoseEmote, null);

            return;
        }

        Switch(type, index);
    }

    /// <summary>
    /// Ein aufgeschobener Wechsel: Der Spieler hat waehrend eines Emotes geklickt.
    /// </summary>
    private static (EmoteController.PoseType Type, byte Index)? pending;

    private static DateTime pendingUntil;

    /// <summary>
    /// Wie lange auf das Ende des Emotes gewartet wird. Bricht es nicht ab - etwa
    /// weil eine Zwischensequenz laeuft - soll der Wunsch nicht ewig anhaengen und
    /// spaeter aus dem Nichts zuschlagen.
    /// </summary>
    private static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Holt einen aufgeschobenen Wechsel nach, sobald das Emote vorbei ist. Gehoert
    /// in den Puls des Widgets, nicht des Popups: Ein Klick auf eine Pose schliesst
    /// die Liste oft mit, der Wechsel muss trotzdem zu Ende laufen.
    /// </summary>
    public static void Tick()
    {
        if (pending is not { } want)
            return;

        if (DateTime.UtcNow > pendingUntil)
        {
            pending = null;
            return;
        }

        // Nicht nur auf das Ende des Emotes warten, sondern darauf, dass die Haltung
        // wieder ablesbar ist. Waehrend eines Emotes steht in CurrentPoseType 0xFF
        // und CPoseState auf 0 statt auf der echten Pose - wer zu frueh rechnet,
        // rechnet von einem falschen Ausgangswert aus.
        if (!HasValidPose())
            return;

        pending = null;

        Switch(want.Type, want.Index);
    }

    private static unsafe void Switch(EmoteController.PoseType type, byte index)
    {
        var player = Control.GetLocalPlayer();

        if (player is null)
            return;

        var controller = &player->Character.EmoteController;

        // Die Haltung kann sich zwischen Zeichnen und Klick geaendert haben - dann
        // waere der Index auf die falsche Reihe gemuenzt.
        if (controller->CurrentPoseType != type || controller->CPoseState == index)
            return;

        var count = PoseCount(type);

        if (index >= count)
            return;

        var steps = (index - controller->CPoseState + count) % count;
        var manager = EmoteManager.Instance();

        for (var i = 0; i < steps; i++)
            manager->ExecuteEmote(ChangePoseEmote, null);
    }

    /// <summary>Name der Haltung fuer die Anzeige.</summary>
    public static string Describe(EmoteController.PoseType type) => type switch
    {
        EmoteController.PoseType.Idle        => Loc.T("Standing", "Stehend"),
        EmoteController.PoseType.WeaponDrawn => Loc.T("Weapon drawn", "Waffe gezogen"),
        EmoteController.PoseType.Sit         => Loc.T("Sitting in a chair", "Auf einem Stuhl sitzend"),
        EmoteController.PoseType.GroundSit   => Loc.T("Sitting on the ground", "Auf dem Boden sitzend"),
        EmoteController.PoseType.Doze        => Loc.T("Dozing", "Dösend"),
        EmoteController.PoseType.Umbrella    => Loc.T("Umbrella", "Regenschirm"),
        EmoteController.PoseType.Accessory   => Loc.T("Accessory", "Accessoire"),
        _                                    => type.ToString(),
    };
}
