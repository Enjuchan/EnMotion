using Dalamud.Plugin.Services;
using EnMotion.Core;
using Lumina.Excel.Sheets;

namespace EnMotion;

public static class EmoteCatalog
{
    /// <summary>
    /// Liest alle benutzbaren Emotes aus den Spieldaten.
    ///
    /// Uebersprungen wird, was in der Liste nichts verloren haette: Eintraege ohne
    /// Namen, ohne Textbefehl oder ohne Animation. Alle drei kommen in der Tabelle
    /// vor - sie enthaelt auch interne und unbenutzte Zeilen.
    /// </summary>
    public static IReadOnlyList<EmoteEntry> LoadAll(IDataManager data)
    {
        var sheet = data.GetExcelSheet<Emote>();

        if (sheet is null)
            return [];

        var result = new List<EmoteEntry>();

        foreach (var emote in sheet)
        {
            var name = emote.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var command = emote.TextCommand.ValueNullable?.Command.ExtractText() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(command))
                continue;

            var keys = new List<string>();

            foreach (var timeline in emote.ActionTimeline)
            {
                var key = timeline.ValueNullable?.Key.ExtractText();
                if (!string.IsNullOrWhiteSpace(key))
                    keys.Add(key);
            }

            if (keys.Count == 0)
                continue;

            result.Add(new EmoteEntry(emote.RowId, name, emote.Icon, command, keys, emote.EmoteCategory.RowId));
        }

        LogCategories(data, result);

        return result;
    }

    /// <summary>
    /// Haelt einmal im Log fest, welche Kategorie-Zeilen mit welchem Namen vorkommen, damit sich
    /// die Zeile der Ausdruecke im Spiel bestaetigen laesst.
    /// </summary>
    private static void LogCategories(IDataManager data, IReadOnlyList<EmoteEntry> entries)
    {
        var names = data.GetExcelSheet<EmoteCategory>();

        var rows = entries
            .GroupBy(e => e.CategoryRow)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var name = names is not null && names.TryGetRow(g.Key, out var row) ? row.Name.ExtractText() : "?";
                return $"row {g.Key} \"{name}\" x{g.Count()}";
            });

        Plugin.Log.Information(
            $"Emote categories: {string.Join(", ", rows)}; expressions are row {EmoteEntry.ExpressionCategoryRow}.");
    }
}
