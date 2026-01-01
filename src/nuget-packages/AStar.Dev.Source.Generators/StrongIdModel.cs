using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators;

internal sealed class StrongIdModel(string? ns, string name, Accessibility accessibility, string underlyingTypeDisplay)
{
    public string? Namespace { get; } = ns;
    public string Name { get; } = name;
    public Accessibility Accessibility { get; } = accessibility;
    public string UnderlyingTypeDisplay { get; } = underlyingTypeDisplay;
}
