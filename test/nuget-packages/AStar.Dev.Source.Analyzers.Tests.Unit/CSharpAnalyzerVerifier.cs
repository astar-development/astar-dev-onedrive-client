using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace AStar.Dev.Source.Analyzers.Tests.Unit;

public static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => new(diagnosticId, DiagnosticSeverity.Error);

    [Obsolete]
    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80 // Use Net80 for .NET 8+, or Net70 for .NET 7, etc.
        };
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
