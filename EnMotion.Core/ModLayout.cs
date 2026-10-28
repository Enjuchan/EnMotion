using System.Text.Json;

namespace EnMotion.Core;

/// <summary>Eine Option einer Gruppe samt der Spielpfade, die sie ersetzt (Files und FileSwaps, klein, mit Schraegstrichen).</summary>
public sealed record ModOption(string Name, int Priority, IReadOnlyList<string> Paths);

/// <summary>Eine Optionsgruppe; <paramref name="Multi"/> heisst Mehrfachauswahl, sonst waehlt die Gruppe genau eine Option.</summary>
public sealed record OptionGroup(string Name, int Priority, bool Multi, IReadOnlyList<ModOption> Options);

/// <summary>Was eine Mod in ihren Dateien ersetzt: die Standarddateien und je Gruppe die Optionen.</summary>
public sealed record ModLayout(IReadOnlyList<string> DefaultPaths, IReadOnlyList<OptionGroup> Groups)
{
    public bool IsMulti(string group) => Groups.FirstOrDefault(g => g.Name == group)?.Multi ?? false;
}

/// <summary>
/// Liest <c>default_mod.json</c> und <c>group_*.json</c> einer Penumbra-Mod. Gruppen vom Typ Single und Multi
/// werden gelesen; andere Typen (Imc, Combining) fuehren ihre Aenderungen nicht als Optionen mit Dateien und
/// bleiben aussen vor.
/// </summary>
public static class ModLayoutReader
{
    /// <param name="defaultJson">Inhalt von default_mod.json oder null.</param>
    /// <param name="groupJsons">Inhalte der Gruppendateien in der Reihenfolge der Dateinamen.</param>
    /// <exception cref="JsonException">Ungueltiges JSON in einer der Dateien.</exception>
    public static ModLayout Parse(string? defaultJson, IEnumerable<string> groupJsons)
    {
        var defaults = defaultJson is null ? [] : ReadPaths(Root(defaultJson));
        var groups = new List<OptionGroup>();

        foreach (var json in groupJsons)
        {
            using var doc = JsonDocument.Parse(Strip(json));

            if (ReadGroup(doc.RootElement) is { } group)
                groups.Add(group);
        }

        return new ModLayout(defaults, groups);
    }

    /// <summary>
    /// Liest das Format FileVersion 4, in dem Penumbra 1.7 alles in <c>meta.json</c> fuehrt: die Standarddateien
    /// im Feld <c>DefaultData</c> (Files, FileSwaps), die Gruppen in der Liste <c>Groups</c> (Type, Name, Priority,
    /// Options mit Name, Priority, Files, FileSwaps). Null, wenn die Datei kein Format 4 ist.
    /// </summary>
    /// <exception cref="JsonException">Ungueltiges JSON.</exception>
    public static ModLayout? ParseMeta(string metaJson)
    {
        using var doc = JsonDocument.Parse(Strip(metaJson));
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("FileVersion", out var version)
            || !version.TryGetInt32(out var number) || number < 4)
            return null;

        var defaults = root.TryGetProperty("DefaultData", out var data) ? ReadPaths(data) : [];
        var groups = new List<OptionGroup>();

        if (root.TryGetProperty("Groups", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in list.EnumerateArray())
            {
                if (ReadGroup(element) is { } group)
                    groups.Add(group);
            }
        }

        return new ModLayout(defaults, groups);
    }

    private static OptionGroup? ReadGroup(JsonElement root)
    {
        var type = Str(root, "Type");
        if (type is not ("Single" or "Multi") || !root.TryGetProperty("Options", out var options)
            || options.ValueKind != JsonValueKind.Array)
            return null;

        var name = Str(root, "Name");
        if (string.IsNullOrEmpty(name))
            return null;

        var list = new List<ModOption>();
        foreach (var option in options.EnumerateArray())
        {
            var optionName = Str(option, "Name");
            if (!string.IsNullOrEmpty(optionName))
                list.Add(new ModOption(optionName, Int(option, "Priority"), ReadPaths(option)));
        }

        return new OptionGroup(name, Int(root, "Priority"), type == "Multi", list);
    }

    private static JsonElement Root(string json)
    {
        using var doc = JsonDocument.Parse(Strip(json));
        return doc.RootElement.Clone();
    }

    // Dateien aus Penumbra tragen mitunter eine UTF-8-Markierung; File.ReadAllText entfernt sie, Zeichenketten von anderswo nicht.
    private static string Strip(string json) => json.TrimStart((char)0xFEFF);

    private static List<string> ReadPaths(JsonElement element)
    {
        var paths = new List<string>();

        foreach (var field in new[] { "Files", "FileSwaps" })
        {
            if (element.TryGetProperty(field, out var map) && map.ValueKind == JsonValueKind.Object)
                paths.AddRange(map.EnumerateObject().Select(p => Normalize(p.Name)));
        }

        return paths;
    }

    public static string Normalize(string path) => path.Replace('\\', '/').ToLowerInvariant();

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static int Int(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : 0;
}
