using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators;

[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "<Pending>")]
public sealed partial class OptionsBindingGenerator : IIncrementalGenerator
{
    private const string AttrFqn = "AStar.Dev.Source.Generators.Attributes.AutoRegisterOptionsAttribute";

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
                predicate: static (node, _) => IsPotentialCandidate(node),
                transform: static (syntaxCtx, _) => OptionsModel.TryCreate(syntaxCtx))
            .Where(static m => m is not null);

    private static bool IsPotentialCandidate(SyntaxNode node) => IsPotentialClassCandidate(node) || IsPotentialStructCandidate(node);

    private static bool IsPotentialStructCandidate(SyntaxNode node) => node is StructDeclarationSyntax s &&
                   s.Modifiers.Any(m => m.Text == "partial") &&
                   s.AttributeLists.Count > 0;

    private static bool IsPotentialClassCandidate(SyntaxNode node) => node is ClassDeclarationSyntax c &&
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
}
