using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace AStar.Dev.Source.Analyzers.Tests.Unit;

public class AutoRegisterOptionsPartialAnalyzerShould
{
    /// <summary>
    /// Helper to create the expected diagnostic result for a missing partial.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <returns>A DiagnosticResult for the analyzer.</returns>
    private static DiagnosticResult ExpectDiagnostic(string className, int line, int column)
    {
        DiagnosticResult result = new DiagnosticResult(AutoRegisterOptionsPartialAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithMessage($"Options class '{className}' must be declared partial to support source generation")
            .WithSpan(line, column, line, column + className.Length);
        return result;
    }

    [Fact]
    public async Task ReportsDiagnostic_WhenClassIsNotPartial()
    {
        var test = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [AutoRegisterOptions]
    public class NotPartialOptions { }
}";
        var expected = ExpectDiagnostic("NotPartialOptions", 5, 18);
        await VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task DoesNotReportDiagnostic_WhenClassIsPartial()
    {
        var test = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [AutoRegisterOptions]
    public partial class PartialOptions { }
}";
        await VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Verifies the analyzer against the provided source and expected diagnostics.
    /// </summary>
    /// <param name="source">The C# source code to analyze.</param>
    /// <param name="expected">Expected diagnostics.</param>
    private static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AutoRegisterOptionsPartialAnalyzer, XUnitVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        };
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
