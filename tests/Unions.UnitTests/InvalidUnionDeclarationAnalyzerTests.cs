using MaVe.Unions.UnitTests.Verifiers;
using MaVe.UnionsAnalyzer;
using Microsoft.CodeAnalysis.Testing;

namespace MaVe.Unions.UnitTests;

[TestFixture]
public class InvalidUnionDeclarationAnalyzerTests
{
    private const string DiagnosticId = "DNHU0003";

    [Test]
    public async Task UnionOnPartialClass_ReportsDiagnostic()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public partial class BusinessParty
                              {
                              }
                              """;

        var expected = CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .Diagnostic(DiagnosticId)
            .WithSpan(5, 2, 5, 7);

        await CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task UnionOnNonPartialRecord_ReportsDiagnostic()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public record BusinessParty
                              {
                              }
                              """;

        var expected = CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .Diagnostic(DiagnosticId)
            .WithSpan(5, 2, 5, 7);

        await CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task UnionOnNonPartialClass_ReportsDiagnostic()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public class BusinessParty
                              {
                              }
                              """;

        var expected = CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .Diagnostic(DiagnosticId)
            .WithSpan(5, 2, 5, 7);

        await CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task UnionOnPartialRecord_DoesNotReportDiagnostic()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public partial record BusinessParty
                              {
                              }
                              """;

        await CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task UnionOnPartialRecordStruct_ReportsDiagnostic()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public partial record struct BusinessParty
                              {
                              }
                              """;

        var expected = CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .Diagnostic(DiagnosticId)
            .WithSpan(5, 2, 5, 7);

        var expectedCompilerDiagnostic = DiagnosticResult
            .CompilerError("CS0592")
            .WithSpan(5, 2, 5, 7)
            .WithArguments("Union", "class");

        await CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .VerifyAnalyzerAsync(source, expected, expectedCompilerDiagnostic);
    }

    [Test]
    public async Task TypeWithoutUnionAttribute_DoesNotReportDiagnostic()
    {
        const string source = """
                              namespace Demo;

                              public partial class BusinessParty
                              {
                              }
                              """;

        await CSharpAnalyzerVerifier<InvalidUnionDeclarationAnalyzer>
            .VerifyAnalyzerAsync(source);
    }
}
