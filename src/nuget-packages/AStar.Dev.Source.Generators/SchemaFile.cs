namespace AStar.Dev.Source.Generators;

public sealed partial class OptionsBindingGenerator
{
    internal sealed class SchemaFile(string path, List<OptionsBindingGenerator.SchemaEntry> entries)
    {
        public string Path { get; } = path;
        public List<SchemaEntry> Entries { get; } = entries;
    }
}
