using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators.StrongIdCodeGeneration;

internal sealed class StrongIdModel(string? namepsace, string modelName, Accessibility accessibility, string underlyingTypeDisplay)
{
    public string? Namespace { get; } = namepsace;
    public string ModelName { get; } = modelName;
    public Accessibility Accessibility { get; } = accessibility;
    public string UnderlyingTypeDisplay { get; } = underlyingTypeDisplay;
}
