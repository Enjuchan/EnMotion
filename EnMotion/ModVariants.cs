using EnMotion.Core;

namespace EnMotion;

/// <summary>
/// Waehlt je Emote eine Variante (Option einer Mod oder Mod als Ganzes) ueber Penumbras vorlaeufige
/// Einstellungen, ohne die gespeicherte Konfiguration zu aendern. Was je Emote gewaehlt ist, steht nur im
/// Speicher; je Mod werden die Wahlen aller Emotes zusammen gesetzt.
/// </summary>
public static class ModVariants
{
    // Namen von PenumbraApiEc, Index = Wert (Penumbra.Api 1.7.2.1).
    private static readonly string[] ErrorNames =
    [
        "Success", "NothingChanged", "CollectionMissing", "ModMissing", "OptionGroupMissing", "OptionMissing",
        "CharacterCollectionExists", "LowerPriority", "InvalidGamePath", "FileMissing", "InvalidManipulation",
        "InvalidArgument", "PathRenameFailed", "CollectionExists", "AssignmentCreationDisallowed",
        "AssignmentDeletionDisallowed", "InvalidIdentifier", "SystemDisposed", "AssignmentDeletionFailed",
        "TemporarySettingDisallowed", "TemporarySettingImpossible", "InvalidCredentials", "CollectionInactive",
    ];

    private static readonly VariantBook book = new();

    /// <summary>Ob EnMotion fuer dieses Emote eine Variante gesetzt hat.</summary>
    public static bool HasChoice(uint emoteId) => book.ChosenFor(emoteId) is not null;

    /// <summary>
    /// Die Variante, die fuer das Emote wirkt: nach Mod-Prioritaet, in der Mod nach Gruppen- und Optionspriorität,
    /// in der Sammlung des eigenen Charakters einschliesslich vorlaeufiger Einstellungen. Null, wenn keine wirkt.
    /// </summary>
    public static Variant? Effective(ModdedEmote emote)
    {
        if (PenumbraBridge.GetOwnCollection() is not { } collection)
            return null;

        var states = new Dictionary<ModRef, ModState?>();
        foreach (var mod in ModdedEmotes.Index.ModsOf(emote.Emote.Name))
            states[mod] = StateOf(collection, mod, ignoreTemporary: false);

        return VariantSettings.Effective(ModdedEmotes.Index.VariantsOf(emote.Emote.Name), mod => states.GetValueOrDefault(mod));
    }

    public static void Choose(ModdedEmote emote, Variant variant)
    {
        if (PenumbraBridge.GetOwnCollection() is not { } collection)
        {
            ReportFailure(PenumbraBridge.Failed);
            return;
        }

        // Erst die Mod der bisherigen Wahl, dann die neue: So rechnet die neue Prioritaet ohne die alte Einstellung.
        foreach (var mod in book.Choose(emote.Emote.Id, emote.Emote.Name, variant))
        {
            var code = Apply(collection, mod);
            if (code != 0)
            {
                book.Release(emote.Emote.Id);
                ReportFailure(code);
                return;
            }
        }
    }

    public static void Reset(ModdedEmote emote)
    {
        if (book.Release(emote.Emote.Id) is not { } mod)
            return;

        if (PenumbraBridge.GetOwnCollection() is not { } collection)
        {
            ReportFailure(PenumbraBridge.Failed);
            return;
        }

        var code = Apply(collection, mod);
        if (code != 0)
            ReportFailure(code);
    }

    /// <summary>Beim Entladen: entfernt alles, was EnMotion in Penumbra gesetzt hat.</summary>
    public static void ResetAll()
    {
        book.Clear();
        PenumbraBridge.RemoveAllTemporary();
    }

    /// <summary>Setzt die Mod mit den Wahlen aller ihrer Emotes zusammen oder entfernt die Einstellung, wenn keine Wahl bleibt.</summary>
    private static int Apply(Guid collection, ModRef mod)
    {
        var choices = book.ChoicesIn(mod);
        if (choices.Count == 0)
            return PenumbraBridge.RemoveTemporary(mod.Directory, mod.Name);

        // Grundlage sind die gespeicherten Einstellungen: Eine fruehere vorlaeufige Einstellung wird neu aus allen Wahlen gebaut.
        var saved = PenumbraBridge.GetModSetting(collection, mod.Directory, mod.Name, ignoreTemporary: true);
        if (saved.Code != 0)
            return saved.Code;

        var index = ModdedEmotes.Index;
        var options = VariantSettings.Apply(
            index.LayoutOf(mod.Directory),
            saved.Options,
            choices.Select(c => (c.EmoteName, c.Variant)),
            emote => index.OptionsOf(mod.Directory, emote));

        // Die Prioritaet muss fuer jedes gewaehlte Emote reichen.
        var priority = choices.Max(c => ModPriority.Target(mod.Directory, Settings(collection, c.EmoteName)));

        return PenumbraBridge.SetTemporary(
            mod.Directory,
            mod.Name,
            priority,
            options.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value));
    }

    private static IEnumerable<ModSetting> Settings(Guid collection, string emoteName)
    {
        foreach (var mod in ModdedEmotes.Index.ModsOf(emoteName))
        {
            var s = PenumbraBridge.GetModSetting(collection, mod.Directory, mod.Name);
            if (s.Code == 0)
                yield return new ModSetting(mod.Directory, s.Enabled, s.Priority);
        }
    }

    private static ModState? StateOf(Guid collection, ModRef mod, bool ignoreTemporary)
    {
        var s = PenumbraBridge.GetModSetting(collection, mod.Directory, mod.Name, ignoreTemporary);
        return s.Code == 0 ? new ModState(s.Enabled, s.Priority, s.Options) : null;
    }

    private static void ReportFailure(int code)
    {
        var detail = code == PenumbraBridge.Failed
            ? PenumbraBridge.LastError ?? "?"
            : $"{code} ({(code >= 0 && code < ErrorNames.Length ? ErrorNames[code] : "Unknown")})";

        Plugin.Chat.PrintError("[EnMotion] " + Loc.T($"Penumbra error: {detail}", $"Penumbra-Fehler: {detail}"));
    }
}
