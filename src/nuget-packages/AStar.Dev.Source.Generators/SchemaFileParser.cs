using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators;

internal static class SchemaFileParser
{
    public static OptionsBindingGenerator.SchemaFile Parse(AdditionalText text)
    {
        var entries = new List<OptionsBindingGenerator.SchemaEntry>();
        var content = text.GetText()?.ToString() ?? "";
        var lines = SplitIntoLines(content);

        foreach(var line in lines)
        {
            OptionsBindingGenerator.SchemaEntry? entry = TryParseSchemaLine(line);
            if(entry is not null)
                entries.Add(entry);
        }

        return new OptionsBindingGenerator.SchemaFile(text.Path, entries);
    }

    private static string[] SplitIntoLines(string content) => content.Split(["\r\n", "\n"], StringSplitOptions.None);

    private static OptionsBindingGenerator.SchemaEntry? TryParseSchemaLine(string raw)
    {
        var line = raw.Trim();

        if(IsEmptyOrComment(line))
            return null;

        var parts = line.Split(['='], 2);
        if(parts.Length != 2)
            return null;

        var left = parts[0].Trim();
        var right = parts[1].Trim();

        (string section, string property)? parsed = ParseSectionAndProperty(left);
        if(parsed is null)
            return null;

        (var isRequired, var defaultValue) = ParseRequiredOrDefault(right);

        return string.IsNullOrEmpty(parsed.Value.section) || string.IsNullOrEmpty(parsed.Value.property)
            ? null
            : new OptionsBindingGenerator.SchemaEntry(
            parsed.Value.section,
            parsed.Value.property,
            isRequired,
            defaultValue);
    }

    private static bool IsEmptyOrComment(string line) => line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal);

    private static (string section, string property)? ParseSectionAndProperty(string left)
    {
        var sp = left.Split([':'], 2);
        return sp.Length != 2 ? null : (sp[0].Trim(), sp[1].Trim());
    }

    private static (bool isRequired, string? defaultValue) ParseRequiredOrDefault(string right)
    {
        if(right.Equals("required", StringComparison.OrdinalIgnoreCase))
            return (true, null);

        if(right.StartsWith("default:", StringComparison.OrdinalIgnoreCase))
            return (false, right.Substring("default:".Length).Trim());

        return (false, null);
    }
}
