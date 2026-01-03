using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = AStar.Dev.Source.Analyzers.Tests.Unit.CSharpAnalyzerVerifier<AStar.Dev.Source.Analyzers.AutoRegisterOptionsPartialAnalyzer>;

namespace AStar.Dev.Source.Analyzers.Tests.Unit;

public class AutoRegisterOptionsPartialAnalyzerShould
{
    [Fact]
    [Obsolete]
    public async Task ReportsDiagnostic_WhenClassIsNotPartial()
    {
        var test = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [AutoRegisterOptions]
    public class NotPartialOptions { }
}";
        DiagnosticResult expected = VerifyCS.Diagnostic(AStar.Dev.Source.Analyzers.AutoRegisterOptionsPartialAnalyzer.DiagnosticId)
            .WithSpan(5, 18, 5, 35)
            .WithArguments("NotPartialOptions");
        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Obsolete]
    public async Task DoesNotReportDiagnostic_WhenClassIsPartial()
    {
        var test = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [AutoRegisterOptions]
    public partial class PartialOptions { }
}";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
