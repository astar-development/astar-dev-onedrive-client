using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AStar.Dev.Source.Generators;

internal static class SchemaDictionaryBuilder
{
    public static Dictionary<string, Dictionary<string, OptionsBindingGenerator.SchemaEntry>> Build(
        ImmutableArray<OptionsBindingGenerator.SchemaFile> files)
    {
        var map = new Dictionary<string, Dictionary<string, OptionsBindingGenerator.SchemaEntry>>(StringComparer.Ordinal);

        foreach (OptionsBindingGenerator.SchemaFile file in files)
        {
            ProcessSchemaFile(map, file);
        }

        return map;
    }

    private static void ProcessSchemaFile(
        Dictionary<string, Dictionary<string, OptionsBindingGenerator.SchemaEntry>> map,
        OptionsBindingGenerator.SchemaFile file)
    {
        foreach (OptionsBindingGenerator.SchemaEntry entry in file.Entries)
        {
            AddEntryToMap(map, entry);
        }
    }

    private static void AddEntryToMap(
        Dictionary<string, Dictionary<string, OptionsBindingGenerator.SchemaEntry>> map,
        OptionsBindingGenerator.SchemaEntry entry)
    {
        if (!map.TryGetValue(entry.Section, out Dictionary<string, OptionsBindingGenerator.SchemaEntry>? props))
        {
            props = new Dictionary<string, OptionsBindingGenerator.SchemaEntry>(StringComparer.Ordinal);
            map[entry.Section] = props;
        }

        props[entry.Property] = entry;
    }
}
