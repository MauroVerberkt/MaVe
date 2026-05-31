using HelperUnions.UnitTests.Verifiers;

namespace HelperUnions.UnitTests;

[TestFixture]
public class NonExhaustiveUnionMatchCodeFixTests
{
    private const string DiagnosticId = "DNHU0001";

    [Test]
    public async Task SwitchExpression_MissingVariants_AddsMissingArms()
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
                    return party {|#0:switch|}
                    {
                        BusinessParty.Customer c => c.Name,
                    };
                }
            }
            """;

        const string fixedSource = """
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
                        BusinessParty.Prospect _ => throw new System.NotImplementedException(),
                        BusinessParty.Supplier _ => throw new System.NotImplementedException()
                    };
                }
            }
            """;

        var expected = CSharpCodeFixVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer, HelperUnionsFixProvider.NonExhaustiveUnionMatchCodeFixProvider>
            .Diagnostic(DiagnosticId)
            .WithLocation(0)
            .WithArguments("Prospect, Supplier");

        await CSharpCodeFixVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer, HelperUnionsFixProvider.NonExhaustiveUnionMatchCodeFixProvider>
            .VerifyCodeFixAsync(source, fixedSource, expected);
    }

    [Test]
    public async Task SwitchStatement_MissingVariant_AddsMissingCaseSection()
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
                    {|#0:switch|} (party)
                    {
                        case BusinessParty.Customer _:
                            break;
                    }
                }
            }
            """;

        const string fixedSource = """
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
                            throw new System.NotImplementedException();
                    }
                }
            }
            """;

        var expected = CSharpCodeFixVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer, HelperUnionsFixProvider.NonExhaustiveUnionMatchCodeFixProvider>
            .Diagnostic(DiagnosticId)
            .WithLocation(0)
            .WithArguments("Supplier");

        await CSharpCodeFixVerifier<HelperUnionsAnalyzer.NonExhaustiveUnionMatchAnalyzer, HelperUnionsFixProvider.NonExhaustiveUnionMatchCodeFixProvider>
            .VerifyCodeFixAsync(source, fixedSource, expected);
    }
}
