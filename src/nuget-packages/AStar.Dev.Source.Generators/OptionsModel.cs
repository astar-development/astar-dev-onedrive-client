using Microsoft.CodeAnalysis;
using static AStar.Dev.Source.Generators.OptionsBindingGenerator;

namespace AStar.Dev.Source.Generators;

internal sealed class OptionsModel(string? @namespace, string typeName, string sectionName, OptionsBindingGenerator.PropModel[] properties)
{
    public string? Namespace { get; } = @namespace;
    public string TypeName { get; } = typeName;
    public string SectionName { get; } = sectionName;
    public PropModel[] Properties { get; } = properties;

    public static OptionsModel? TryCreate(GeneratorAttributeSyntaxContext syntaxCtx)
    {
        var type = (INamedTypeSymbol)syntaxCtx.TargetSymbol;

        if(!IsValidOptionsType(type))
            return null;

        var section = ExtractSectionName(syntaxCtx.Attributes[0]);
        PropModel[] props = CollectProperties(type);
        var ns = GetNamespace(type);

        return new OptionsModel(ns, type.Name, section, props);
    }

    private static bool IsValidOptionsType(INamedTypeSymbol type) => type.DeclaredAccessibility == Accessibility.Public &&
               !type.IsAbstract &&
               type.Arity == 0;

    private static string ExtractSectionName(AttributeData attr) => attr.ConstructorArguments.Length == 1 &&
            attr.ConstructorArguments[0].Value is string s &&
            !string.IsNullOrWhiteSpace(s)
            ? s
            : "Options";

    private static PropModel[] CollectProperties(INamedTypeSymbol type) => [.. type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                .Select(p => new PropModel(p.Name, GetSimpleKind(p.Type)))];

    private static string? GetNamespace(INamedTypeSymbol type) => type.ContainingNamespace.IsGlobalNamespace
            ? null
            : type.ContainingNamespace.ToDisplayString();
}
