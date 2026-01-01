using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStar.Dev.Source.Generators.Tests;

public class ServiceRegistrationGeneratorShould
{
    private const string AttributeSource = @"using System;\nnamespace AStar.Dev.Source.Generators.Attributes {\n    public enum ServiceLifetime { Singleton, Scoped, Transient }\n    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]\n    public sealed class ServiceAttribute : Attribute {\n        public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) { Lifetime = lifetime; }\n        public ServiceLifetime Lifetime { get; }\n        public Type? As { get; set; }\n        public bool AsSelf { get; set; } = false;\n    }\n}";

    private static CSharpCompilation CreateCompilation(string input)
    {
        PortableExecutableReference diReference = MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location);
        return CSharpCompilation.Create("TestAssembly",
            [
                CSharpSyntaxTree.ParseText(AttributeSource),
                CSharpSyntaxTree.ParseText(input)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                diReference
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void RegisterClassWithSingleInterface_DefaultScoped()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} [Service] public class Foo : IFoo {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var text = generated.SourceText.ToString();

        text.ShouldContain("s.AddScoped<global::TestNamespace.IFoo, global::TestNamespace.Foo>();");
    }

    [Fact]
    public void RegisterClassWithSingleInterface_SingletonLifetime()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} [Service(ServiceLifetime.Singleton)] public class Foo : IFoo {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var text = generated.SourceText.ToString();

        text.ShouldContain("s.AddSingleton<global::TestNamespace.IFoo, global::TestNamespace.Foo>();");
    }

    [Fact]
    public void RegisterClassWithSingleInterface_AsSelfTrue()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} [Service(AsSelf = true)] public class Foo : IFoo {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var text = generated.SourceText.ToString();

        text.ShouldContain("s.AddScoped<global::TestNamespace.IFoo, global::TestNamespace.Foo>();");
        text.ShouldContain("s.AddScoped<global::TestNamespace.Foo>();");
    }

    [Fact]
    public void RegisterClassWithSingleInterface_AsOverride()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} public interface IBar {} [Service(As = typeof(IBar))] public class Foo : IFoo, IBar {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var text = generated.SourceText.ToString();

        text.ShouldContain("s.AddScoped<global::TestNamespace.IBar, global::TestNamespace.Foo>();");
    }

    [Fact]
    public void RegisterClassWithNoInterface_AsSelfTrue()
    {
        const string input = @"namespace TestNamespace { [Service(AsSelf = true)] public class Foo {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var text = generated.SourceText.ToString();

        text.ShouldContain("s.AddScoped<global::TestNamespace.Foo>();");
    }

    [Fact]
    public void DoesNotRegisterAbstractOrNonPublicOrGenericClasses()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} [Service] public abstract class AbstractFoo : IFoo {} [Service] internal class InternalFoo : IFoo {} [Service] public class GenericFoo<T> : IFoo {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var text = generated.SourceText.ToString();

        text.ShouldNotContain("AbstractFoo");
        text.ShouldNotContain("InternalFoo");
        text.ShouldNotContain("GenericFoo");
    }

    [Fact]
    public void DoesNotRegisterClassWithoutServiceAttribute()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} public class Foo : IFoo {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));

        if(!generated.Equals(default(GeneratedSourceResult)))
        {
            var text = generated.SourceText.ToString();

            text.ShouldNotContain("Foo");
        }
    }

    [Fact]
    public void DoesNotRegisterClassWithMultipleInterfacesAndNoAsSpecified()
    {
        const string input = @"namespace TestNamespace { public interface IFoo {} public interface IBar {} [Service] public class Foo : IFoo, IBar {} }";
        CSharpCompilation compilation = CreateCompilation(input);
        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult generated = result.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault(x => x.HintName.Contains("ServiceCollectionExtensions"));

        if(!generated.Equals(default(GeneratedSourceResult)))
        {
            var text = generated.SourceText.ToString();

            text.ShouldNotContain("Foo");
        }
    }
}
