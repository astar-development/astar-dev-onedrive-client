using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators;

[Generator]
public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol?> classSyntax = CreateClassSyntaxProvider(context);
        IncrementalValuesProvider<(INamedTypeSymbol sym, AttributeData? attr)> services = CreateServicesProvider(classSyntax);
        IncrementalValuesProvider<ServiceModel?> serviceModels = CreateServiceModelsProvider(services);
        IncrementalValueProvider<(Compilation Left, ImmutableArray<ServiceModel?> Right)> combined = context.CompilationProvider.Combine(serviceModels.Collect());

        context.RegisterSourceOutput(combined, GenerateSource);
    }

    private static IncrementalValuesProvider<INamedTypeSymbol?> CreateClassSyntaxProvider(
        IncrementalGeneratorInitializationContext ctx) => ctx.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsClassCandidateForServiceRegistration(node),
                transform: static (syntaxCtx, _) => GetDeclaredSymbol(syntaxCtx))
            .Where(static s => s is not null)!;

    private static bool IsClassCandidateForServiceRegistration(SyntaxNode node) => node is ClassDeclarationSyntax c &&
               c.AttributeLists.Count > 0 &&
               c.TypeParameterList is null;

    private static INamedTypeSymbol? GetDeclaredSymbol(GeneratorSyntaxContext syntaxCtx)
    {
        var classDecl = (ClassDeclarationSyntax)syntaxCtx.Node;
        return syntaxCtx.SemanticModel.GetDeclaredSymbol(classDecl);
    }

    private static IncrementalValuesProvider<(INamedTypeSymbol sym, AttributeData? attr)> CreateServicesProvider(
        IncrementalValuesProvider<INamedTypeSymbol?> classSyntax) => classSyntax
            .Select(static (sym, _) => (sym, attr: FindServiceAttribute(sym!)))
            .Where(static t => t.attr is not null)!;

    private static AttributeData? FindServiceAttribute(INamedTypeSymbol symbol) => symbol.GetAttributes()
        .FirstOrDefault(a =>
            a.AttributeClass?.Name == "ServiceAttribute" ||
            a.AttributeClass?.ToDisplayString().EndsWith(".ServiceAttribute") == true);

    private static IncrementalValuesProvider<ServiceModel?> CreateServiceModelsProvider(
        IncrementalValuesProvider<(INamedTypeSymbol sym, AttributeData? attr)> services) => services
            .Select(static (t, _) => ServiceModel.TryCreate(t.sym, t.attr!))
            .Where(static m => m is not null)!;

    private static void GenerateSource(
        SourceProductionContext spc,
        (Compilation Left, ImmutableArray<ServiceModel?> Right) pair)
    {
        var code = ServiceCollectionCodeGenerator.Generate(pair.Right);
        spc.AddSource("GeneratedServiceCollectionExtensions.g.cs", code);
    }

    internal enum Lifetime { Singleton = 0, Scoped = 1, Transient = 2 }

    internal sealed class ServiceModel(ServiceRegistrationGenerator.Lifetime lifetime, string implFqn, string? serviceFqn, bool alsoAsSelf)
    {
        public Lifetime Lifetime { get; } = lifetime;
        public string ImplFqn { get; } = implFqn;
        public string? ServiceFqn { get; } = serviceFqn;
        public bool AlsoAsSelf { get; } = alsoAsSelf;

        public static ServiceModel? TryCreate(INamedTypeSymbol impl, AttributeData attr)
        {
            if(!IsValidImplementationType(impl))
                return null;

            Lifetime lifetime = ExtractLifetime(attr);
            INamedTypeSymbol? asType = ExtractAsType(attr);
            var asSelf = ExtractAsSelf(attr);
            INamedTypeSymbol? service = asType ?? InferServiceType(impl);

            // Only skip if no service and not alsoAsSelf
            return service is null && !asSelf
                ? null
                : new ServiceModel(
                lifetime: lifetime,
                implFqn: impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                serviceFqn: service?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                alsoAsSelf: asSelf
            );
        }

        private static bool IsValidImplementationType(INamedTypeSymbol impl) => !impl.IsAbstract &&
                   impl.Arity == 0 &&
                   impl.DeclaredAccessibility == Accessibility.Public;

        private static Lifetime ExtractLifetime(AttributeData attr) => attr.ConstructorArguments.Length == 1 &&
                   attr.ConstructorArguments[0].Value is int li
                ? (Lifetime)li
                : Lifetime.Scoped;

        private static INamedTypeSymbol? ExtractAsType(AttributeData attr)
        {
            foreach(KeyValuePair<string, TypedConstant> na in attr.NamedArguments)
            {
                if(na.Key == "As" && na.Value.Value is INamedTypeSymbol ts)
                    return ts;
            }

            return null;
        }

        private static bool ExtractAsSelf(AttributeData attr)
        {
            foreach(KeyValuePair<string, TypedConstant> na in attr.NamedArguments)
            {
                if(na.Key == "AsSelf" && na.Value.Value is bool b)
                    return b;
            }

            return false;
        }

        private static INamedTypeSymbol? InferServiceType(INamedTypeSymbol impl)
        {
            INamedTypeSymbol[] candidates = [.. impl.AllInterfaces.Where(IsEligibleServiceInterface)];

            return candidates.Length == 1 ? candidates[0] : null;
        }

        private static bool IsEligibleServiceInterface(INamedTypeSymbol i) => i.DeclaredAccessibility == Accessibility.Public &&
                   i.TypeKind == TypeKind.Interface &&
                   i.Arity == 0 &&
                   i.ToDisplayString() != "System.IDisposable";
    }
}
