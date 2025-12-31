using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators;

[Generator]
public sealed class StrongIdGenerator : IIncrementalGenerator
{
    private const string AttrFqn = "StrongIdAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<StrongIdModel> candidates = CreateStrongIdModelProvider(context);
        IncrementalValueProvider<ImmutableArray<StrongIdModel>> models = candidates
            .WithComparer(StrongIdModelEqualityComparer.Instance)
            .Collect();

        context.RegisterSourceOutput(models, GenerateStrongIdSources);
    }

    private static IncrementalValuesProvider<StrongIdModel> CreateStrongIdModelProvider(
        IncrementalGeneratorInitializationContext ctx) => ctx.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: AttrFqn,
            predicate: static (node, _) => IsPartialStructCandidate(node),
            transform: static (syntaxCtx, _) => StrongIdModel.Create(syntaxCtx));

    private static bool IsPartialStructCandidate(SyntaxNode node) => node is StructDeclarationSyntax s &&
               s.Modifiers.Any(m => m.Text == "partial");

    private static void GenerateStrongIdSources(SourceProductionContext spc, ImmutableArray<StrongIdModel> batch)
    {
        foreach(StrongIdModel? model in batch)
        {
            var code = StrongIdCodeGenerator.Generate(model);
            spc.AddSource($"{model.Name}.StrongId.g.cs", code);
        }
    }

    internal sealed class StrongIdModel(string? ns, string name, Accessibility accessibility, string underlyingTypeDisplay)
    {
        public string? Namespace { get; } = ns;
        public string Name { get; } = name;
        public Accessibility Accessibility { get; } = accessibility;
        public string UnderlyingTypeDisplay { get; } = underlyingTypeDisplay;

        public static StrongIdModel Create(GeneratorAttributeSyntaxContext syntaxCtx)
        {
            var symbol = (INamedTypeSymbol)syntaxCtx.TargetSymbol;
            AttributeData attr = syntaxCtx.Attributes[0];

            var underlyingType = ExtractUnderlyingType(attr);
            var ns = GetNamespace(symbol);

            return new StrongIdModel(ns, symbol.Name, symbol.DeclaredAccessibility, underlyingType);
        }

        private static string ExtractUnderlyingType(AttributeData attr) => attr.ConstructorArguments.Length == 1
                ? (attr.ConstructorArguments[0].Value?.ToString() ?? "System.Guid")
                : "System.Guid";

        private static string? GetNamespace(INamedTypeSymbol symbol) => symbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : symbol.ContainingNamespace.ToDisplayString();
    }

    internal sealed class StrongIdModelEqualityComparer : IEqualityComparer<StrongIdModel>
    {
        public static readonly StrongIdModelEqualityComparer Instance = new();

        public bool Equals(StrongIdModel? x, StrongIdModel? y)
            => ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.Namespace, y.Namespace, StringComparison.Ordinal) &&
                   string.Equals(x.Name, y.Name, StringComparison.Ordinal) &&
                   string.Equals(x.UnderlyingTypeDisplay, y.UnderlyingTypeDisplay, StringComparison.Ordinal) &&
                   x.Accessibility == y.Accessibility);

        public int GetHashCode(StrongIdModel obj) => (obj.Namespace, obj.Name, obj.UnderlyingTypeDisplay, obj.Accessibility).GetHashCode();
    }
}
