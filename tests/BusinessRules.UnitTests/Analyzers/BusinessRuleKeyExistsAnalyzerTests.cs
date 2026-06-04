using BusinessRules.UnitTests.Verifiers;
using BusinessRulesAnalyzer;

namespace BusinessRules.UnitTests.Analyzers;

[TestFixture]
public class BusinessRuleKeyExistsAnalyzerTests
{
    private const string BusinessRulesJson = """
                                             {
                                               "businessRules": [
                                                 {
                                                   "key": "USER_AUTH",
                                                   "description": "User must be authenticated"
                                                 },
                                                 {
                                                   "key": "USER_ADMIN",
                                                   "description": "User must be admin"
                                                 }
                                               ]
                                             }
                                             """;

    [Test]
    public async Task ValidRuleKey_InBusinessRuleAttribute_NoDiagnostic()
    {
        const string test = """
                            using BusinessRules.Attributes;

                            public class TestClass
                            {
                                [BusinessRule("USER_AUTH")]
                                public void TestMethod() { }
                            }
                            """;

        var verifier = new CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>.Test { TestCode = test };
        verifier.AddBusinessRulesJson(BusinessRulesJson);

        await verifier.RunAsync();
    }

    [Test]
    public async Task ValidRuleKey_InImplementsBusinessRuleAttribute_NoDiagnostic()
    {
        const string test = """
                            using BusinessRules.Attributes;

                            public class TestClass
                            {
                                [ImplementsBusinessRule("USER_AUTH")]
                                public void ValidateAuth() { }
                            }
                            """;

        var verifier = new CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>.Test { TestCode = test };
        verifier.AddBusinessRulesJson(BusinessRulesJson);

        await verifier.RunAsync();
    }

    [Test]
    public async Task InvalidRuleKey_InBusinessRuleAttribute_ReportsDiagnostic()
    {
        const string test = """
                            using BusinessRules.Attributes;

                            public class TestClass
                            {
                                [BusinessRule({|#0:"INVALID_KEY"|})]
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>
            .Diagnostic("BR001")
            .WithLocation(0)
            .WithArguments("INVALID_KEY");

        var verifier = new CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>.Test
        {
            TestCode = test, ExpectedDiagnostics = { expected }
        };
        verifier.AddBusinessRulesJson(BusinessRulesJson);

        await verifier.RunAsync();
    }

    [Test]
    public async Task InvalidRuleKey_InImplementsBusinessRuleAttribute_ReportsDiagnostic()
    {
        const string test = """
                            using BusinessRules.Attributes;

                            public class TestClass
                            {
                                [ImplementsBusinessRule({|#0:"MISSING_RULE"|})]
                                public void ValidateRule() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>
            .Diagnostic("BR001")
            .WithLocation(0)
            .WithArguments("MISSING_RULE");

        var verifier = new CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>.Test
        {
            TestCode = test, ExpectedDiagnostics = { expected }
        };
        verifier.AddBusinessRulesJson(BusinessRulesJson);

        await verifier.RunAsync();
    }

    [Test]
    public async Task InvalidRuleKey_OnClass_ReportsDiagnostic()
    {
        const string test = """
                            using BusinessRules.Attributes;

                            [BusinessRule({|#0:"NONEXISTENT"|})]
                            public class TestClass
                            {
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>
            .Diagnostic("BR001")
            .WithLocation(0)
            .WithArguments("NONEXISTENT");

        var verifier = new CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>.Test
        {
            TestCode = test, ExpectedDiagnostics = { expected }
        };
        verifier.AddBusinessRulesJson(BusinessRulesJson);

        await verifier.RunAsync();
    }

    [Test]
    public async Task NoBusinessRulesJson_ReportsDiagnosticForAllKeys()
    {
        const string test = """
                            using BusinessRules.Attributes;

                            public class TestClass
                            {
                                [BusinessRule({|#0:"ANY_KEY"|})]
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>
            .Diagnostic("BR001")
            .WithLocation(0)
            .WithArguments("ANY_KEY");

        var verifier = new CSharpAnalyzerVerifier<BusinessRuleKeyExistsAnalyzer>.Test
        {
            TestCode = test, ExpectedDiagnostics = { expected }
        };
        // No JSON file added, so no keys are defined

        await verifier.RunAsync();
    }
}
