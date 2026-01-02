namespace AStar.Dev.Source.Generators;

public sealed partial class OptionsBindingGenerator
{
    internal sealed class SchemaEntry(string section, string property, bool isRequired, string? defaultValue)
    {
        public string Section { get; } = section;
        public string Property { get; } = property;
        public bool IsRequired { get; } = isRequired;
        public string? DefaultValue { get; } = defaultValue;
    }
}
