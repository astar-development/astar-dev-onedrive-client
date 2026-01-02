namespace AStar.Dev.Source.Generators;

public sealed partial class OptionsBindingGenerator
{
    internal sealed class PropModel(string name, OptionsBindingGenerator.SimpleKind kind)
    {
        public string Name { get; } = name;
        public SimpleKind Kind { get; } = kind;
    }
}
