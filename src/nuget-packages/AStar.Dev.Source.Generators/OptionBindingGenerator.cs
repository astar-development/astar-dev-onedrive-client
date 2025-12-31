using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators;

[Generator]
public sealed class OptionsBindingGenerator : IIncrementalGenerator
{
    private const string AttrFqn = "AStar.Dev.Source.Generators.Annotations.ConfigSectionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<OptionsModel?> optionsTypes = CreateOptionsModelProvider(context);
        IncrementalValueProvider<ImmutableArray<SchemaFile>> schemaFiles = CreateSchemaFilesProvider(context);
        IncrementalValuesProvider<(OptionsModel? Left, ImmutableArray<SchemaFile> Right)> paired = optionsTypes.Combine(schemaFiles);

        context.RegisterSourceOutput(paired, GenerateOptionsBindingSource);
    }

    private static IncrementalValuesProvider<OptionsModel?> CreateOptionsModelProvider(
        IncrementalGeneratorInitializationContext ctx) => ctx.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: AttrFqn,
                predicate: static (node, _) => IsPartialClassCandidate(node),
                transform: static (syntaxCtx, _) => OptionsModel.TryCreate(syntaxCtx))
            .Where(static m => m is not null);

    private static bool IsPartialClassCandidate(SyntaxNode node) => node is ClassDeclarationSyntax c &&
               c.Modifiers.Any(m => m.Text == "partial") &&
               c.AttributeLists.Count > 0;

    private static IncrementalValueProvider<ImmutableArray<SchemaFile>> CreateSchemaFilesProvider(
        IncrementalGeneratorInitializationContext ctx) => ctx.AdditionalTextsProvider
            .Where(static f => f.Path.EndsWith("options.schema", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, ct) => SchemaFileParser.Parse(text))
            .Collect();

    private static void GenerateOptionsBindingSource(
        SourceProductionContext spc,
        (OptionsModel? Left, ImmutableArray<SchemaFile> Right) pair)
    {
        (OptionsModel? model, ImmutableArray<SchemaFile> allSchemas) = pair;
        Dictionary<string, Dictionary<string, SchemaEntry>> dict = SchemaDictionaryBuilder.Build(allSchemas);
        var code = OptionsBindingCodeGenerator.Generate(model!, dict);
        spc.AddSource($"{model!.TypeName}.OptionsBinding.g.cs", code);
    }

    internal enum SimpleKind { String, Int32, Boolean, Other }

    internal static SimpleKind GetSimpleKind(ITypeSymbol t) => t switch
    {
        { SpecialType: SpecialType.System_String } => SimpleKind.String,
        { SpecialType: SpecialType.System_Int32 } => SimpleKind.Int32,
        { SpecialType: SpecialType.System_Boolean } => SimpleKind.Boolean,
        _ => SimpleKind.Other,
    };

    internal sealed class PropModel(string name, OptionsBindingGenerator.SimpleKind kind)
    {
        public string Name { get; } = name;
        public SimpleKind Kind { get; } = kind;
    }

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

    internal sealed class SchemaFile(string path, List<OptionsBindingGenerator.SchemaEntry> entries)
    {
        public string Path { get; } = path;
        public List<SchemaEntry> Entries { get; } = entries;
    }

    internal sealed class SchemaEntry(string section, string property, bool isRequired, string? defaultValue)
    {
        public string Section { get; } = section;
        public string Property { get; } = property;
        public bool IsRequired { get; } = isRequired;
        public string? DefaultValue { get; } = defaultValue;
    }
}
