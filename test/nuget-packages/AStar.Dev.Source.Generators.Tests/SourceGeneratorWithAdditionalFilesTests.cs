using System.IO;
using System.Linq;
using AStar.Dev.Source.Generators.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStar.Dev.Source.Generators.Tests;

public class SourceGeneratorWithAdditionalFilesTests
{
    private const string DddRegistryText = @"User
Document
Customer";
    private static readonly string[] Expected = ["User.g.cs", "Document.g.cs", "Customer.g.cs"];

    [Fact]
    public void GenerateClassesBasedOnDDDRegistry()
    {
        // Create an instance of the source generator.
        var generator = new SourceGeneratorWithAdditionalFiles();

        // Source generators should be tested using 'GeneratorDriver'.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Add the additional file separately from the compilation.
        driver = driver.AddAdditionalTexts(
            [new TestAdditionalFile("./DDD.UbiquitousLanguageRegistry.txt", DddRegistryText)]
        );

        // To run generators, we can use an empty compilation.
        var compilation = CSharpCompilation.Create(nameof(SourceGeneratorWithAdditionalFilesTests));

        // Run generators. Don't forget to use the new compilation rather than the previous one.
        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? newCompilation, out _, TestContext.Current.CancellationToken);

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .ToArray();

        // In this case, it is enough to check the file name.
        Assert.Equivalent(Expected, generatedFiles);
    }
}
