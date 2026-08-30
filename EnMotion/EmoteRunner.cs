using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace EnMotion;

/// <summary>
/// Loest Emotes aus.
///
/// **Direkt ueber `AgentEmote`, nicht ueber die Chatbox.** EnEmotes nimmt dafuer
/// Umbras `IChatSender`, der einen Befehl in die Eingabezeile schreibt. Dalamud bietet
/// dafuer nichts an; man braeuchte einen Signatur-Hook oder eine Fremdbibliothek, und
/// beides bricht nach Patches.
///
/// Der direkte Weg liefert ausserdem zwei Angaben mit, die es sonst nicht gaebe: ob
/// ein Emote gerade benutzbar und ob es ueberhaupt freigeschaltet ist.
/// </summary>
public static unsafe class EmoteRunner
{
    public static void Play(uint emoteId)
    {
        var agent = AgentEmote.Instance();

        if (agent is null)
            return;

        agent->ExecuteEmote((ushort)emoteId, null, false, false);
    }

    /// <summary>Ob das Emote gerade ausgefuehrt werden kann.</summary>
    public static bool CanPlay(uint emoteId)
    {
        var agent = AgentEmote.Instance();

        return agent is not null && agent->CanUseEmote((ushort)emoteId);
    }

    /// <summary>
    /// Ob das Emote freigeschaltet ist. Nicht freigeschaltete werden ausgegraut,
    /// statt ins Leere klicken zu lassen.
    /// </summary>
    public static bool IsUnlocked(uint emoteId)
    {
        var state = UIState.Instance();

        return state is not null && state->IsEmoteUnlocked((ushort)emoteId);
    }
}
