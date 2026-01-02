using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators.OptionsBindingGeneration;

[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "<Pending>")]
public sealed partial class OptionsBindingGenerator : IIncrementalGenerator
{
    private const string AttrFqn = "AStar.Dev.Source.Generators.Attributes.AutoRegisterOptionsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<OptionsTypeInfo?>> optionsTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttrFqn,
            static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            static (ctx, _) => GetOptionsTypeInfo(ctx)
        ).Collect();

        context.RegisterSourceOutput(optionsTypes, static (spc, types) =>
        {
            var validTypes = new List<OptionsTypeInfo>();
            foreach(OptionsTypeInfo? info in types)
            {
                if(info == null)
                    continue;
                // DEBUG: Output type and section name for diagnostics
                Debug.WriteLine($"[OptionsBindingGenerator] Type: {info.FullTypeName}, SectionName: '{info.SectionName}'");
                if(string.IsNullOrWhiteSpace(info.SectionName))
                {
                    var diag = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "ASTAROPT001",
                            title: "Missing Section Name",
                            messageFormat: $"Options class '{info.TypeName}' must specify a section name via the attribute or a static SectionName const field.",
                            category: "AStar.Dev.Source.Generators",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        info.Location);
                    spc.ReportDiagnostic(diag);
                    continue;
                }

                validTypes.Add(info);
            }

            if(validTypes.Count == 0)
                return;
            var code = OptionsBindingCodeGenerator.Generate(validTypes);
            spc.AddSource("AutoOptionsRegistrationExtensions.g.cs", code);
        });
    }

    private static OptionsTypeInfo? GetOptionsTypeInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if(ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;
        var typeName = typeSymbol.Name;
        var ns = typeSymbol.ContainingNamespace?.ToDisplayString();
        var fullTypeName = ns != null ? string.Concat(ns, ".", typeName) : typeName;
        string? sectionName = null;
        AttributeData? attr = typeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttrFqn);
        if(attr != null && attr.ConstructorArguments.Length > 0)
        {
            TypedConstant arg = attr.ConstructorArguments[0];
            if(arg.Kind == TypedConstantKind.Primitive && arg.Value is string s && !string.IsNullOrWhiteSpace(s))
            {
                sectionName = s;
            }
        }

        if(string.IsNullOrWhiteSpace(sectionName))
        {
            foreach(ISymbol member in typeSymbol.GetMembers())
            {
                if(member is IFieldSymbol field && field.IsStatic && field.IsConst && field.Name == "SectionName" && field.Type.SpecialType == SpecialType.System_String && field.ConstantValue is string val && !string.IsNullOrWhiteSpace(val))
                {
                    sectionName = val;
                    break;
                }
            }
        }
        // DEBUG: Output attribute and field detection
        Debug.WriteLine($"[OptionsBindingGenerator] {fullTypeName}: Attribute found: {attr != null}, SectionName: '{sectionName}'");
        return new OptionsTypeInfo(typeName, fullTypeName, sectionName ?? string.Empty, ctx.TargetNode.GetLocation());
    }

    public sealed class OptionsTypeInfo : IEquatable<OptionsTypeInfo>
    {
        public string TypeName { get; }
        public string FullTypeName { get; }
        public string SectionName { get; }
        public Location Location { get; }

        public OptionsTypeInfo(string typeName, string fullTypeName, string sectionName, Location location)
        {
            TypeName = typeName ?? string.Empty;
            FullTypeName = fullTypeName ?? string.Empty;
            SectionName = sectionName;
            Location = location;
        }

        public override bool Equals(object obj) => Equals((OptionsTypeInfo)obj);

        public bool Equals(OptionsTypeInfo other) => ReferenceEquals(this, other) || (other is not null && string.Equals(TypeName, other.TypeName, System.StringComparison.Ordinal)
                && string.Equals(FullTypeName, other.FullTypeName, System.StringComparison.Ordinal)
                && string.Equals(SectionName, other.SectionName, System.StringComparison.Ordinal)
                && Equals(Location, other.Location));
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + (TypeName != null ? TypeName.GetHashCode() : 0);
                hash = (hash * 23) + (FullTypeName != null ? FullTypeName.GetHashCode() : 0);
                hash = (hash * 23) + (SectionName != null ? SectionName.GetHashCode() : 0);
                hash = (hash * 23) + (Location != null ? Location.GetHashCode() : 0);
                return hash;
            }
        }
        public override string ToString() => $"{FullTypeName} (Section: {SectionName})";
    }
}
