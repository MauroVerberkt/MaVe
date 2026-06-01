using HelperUnions.UnitTests.Verifiers;

namespace HelperUnions.UnitTests;

[TestFixture]
public class NonExhaustiveUnionMatchAnalyzerTests
{
    private const string DiagnosticId = "DNHU0001";

    [Test]
    public async Task SwitchStatement_MissingVariants_ReportsDiagnostic()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
                public sealed record Supplier(string CompanyName) : BusinessParty;
                public sealed record Prospect() : BusinessParty;
            }

            public static class UseCase
            {
                public static void Handle(BusinessParty party)
                {
                    switch (party)
                    {
                        case BusinessParty.Customer _:
                            break;
                    }
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .Diagnostic(DiagnosticId)
            .WithSpan(17, 9, 17, 15)
            .WithArguments("Prospect, Supplier");

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task SwitchStatement_WithDefault_DoesNotReportDiagnostic()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
                public sealed record Supplier(string CompanyName) : BusinessParty;
            }

            public static class UseCase
            {
                public static void Handle(BusinessParty party)
                {
                    switch (party)
                    {
                        case BusinessParty.Customer _:
                            break;
                        default:
                            break;
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task SwitchExpression_MissingVariants_ReportsDiagnostic()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
                public sealed record Supplier(string CompanyName) : BusinessParty;
                public sealed record Prospect() : BusinessParty;
            }

            public static class UseCase
            {
                public static string Handle(BusinessParty party)
                {
                    return party switch
                    {
                        BusinessParty.Customer c => c.Name,
                    };
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .Diagnostic(DiagnosticId)
            .WithSpan(17, 22, 17, 28)
            .WithArguments("Prospect, Supplier");

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source, expected);
    }

    [Test]
    public async Task SwitchExpression_WithDiscard_DoesNotReportDiagnostic()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
                public sealed record Supplier(string CompanyName) : BusinessParty;
            }

            public static class UseCase
            {
                public static string Handle(BusinessParty party)
                {
                    return party switch
                    {
                        BusinessParty.Customer c => c.Name,
                        _ => string.Empty,
                    };
                }
            }
            """;

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task SwitchExpression_WithTypePatternForZeroPayloadVariant_DoesNotReportDiagnostic()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
                public sealed record Prospect() : BusinessParty;
            }

            public static class UseCase
            {
                public static string Handle(BusinessParty party)
                {
                    return party switch
                    {
                        BusinessParty.Customer c => c.Name,
                        BusinessParty.Prospect => string.Empty,
                    };
                }
            }
            """;

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task SwitchStatement_AllVariantsCovered_DoesNotReportDiagnostic()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
                public sealed record Supplier(string CompanyName) : BusinessParty;
            }

            public static class UseCase
            {
                public static void Handle(BusinessParty party)
                {
                    switch (party)
                    {
                        case BusinessParty.Customer _:
                            break;
                        case BusinessParty.Supplier _:
                            break;
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task SwitchOnNonUnionType_DoesNotReportDiagnostic()
    {
        const string source = """
            namespace Demo;

            public static class UseCase
            {
                public static void Handle(int value)
                {
                    switch (value)
                    {
                        case 1:
                            break;
                    }
                }
            }
            """;

        await CSharpAnalyzerVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer>
            .VerifyAnalyzerAsync(source);
    }
}
