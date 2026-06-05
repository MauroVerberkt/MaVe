using MaVe.BusinessRules.UnitTests.Verifiers;
using MaVe.BusinessRulesAnalyzer;
using Microsoft.CodeAnalysis;

namespace MaVe.BusinessRules.UnitTests.Analyzers;

[TestFixture]
public class RequiresValidationAnalyzerTests
{
    [Test]
    public async Task BusinessRuleWithValidator_NoDiagnostic()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            public class Validators
                            {
                                [ImplementsBusinessRule("USER_AUTH")]
                                public void ValidateAuth() { }
                            }

                            public class UsageClass
                            {
                                [BusinessRule("USER_AUTH")]
                                public void TestMethod() { }
                            }
                            """;

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test);
    }

    [Test]
    public async Task BusinessRuleWithoutValidator_EnforceTrue_ReportsError()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            public class UsageClass
                            {
                                [BusinessRule({|#0:"MISSING_RULE"|}, enforceValidation: true)]
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<RequiresValidationAnalyzer>
            .Diagnostic("BR002")
            .WithArguments("MISSING_RULE")
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(5, 6, 5, 59);

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task BusinessRuleWithoutValidator_DefaultEnforce_ReportsError()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            public class UsageClass
                            {
                                [BusinessRule({|#0:"MISSING_RULE"|})]
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<RequiresValidationAnalyzer>
            .Diagnostic("BR002")
            .WithArguments("MISSING_RULE")
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(5, 6, 5, 34);

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task BusinessRuleWithoutValidator_EnforceFalse_ReportsWarning()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            public class UsageClass
                            {
                                [BusinessRule({|#0:"OPTIONAL_RULE"|}, enforceValidation: false)]
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<RequiresValidationAnalyzer>
            .Diagnostic("BR003")
            .WithArguments("OPTIONAL_RULE")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithSpan(5, 6, 5, 61);

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task BusinessRuleOnClass_WithoutValidator_ReportsError()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            [BusinessRule({|#0:"CLASS_RULE"|})]
                            public class UsageClass
                            {
                                public void TestMethod() { }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<RequiresValidationAnalyzer>
            .Diagnostic("BR002")
            .WithArguments("CLASS_RULE")
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(3, 2, 3, 28);

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test, expected);
    }

    [Test]
    public async Task MultipleBusinessRules_SomeWithValidators_ReportsOnlyMissing()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            public class Validators
                            {
                                [ImplementsBusinessRule("RULE_A")]
                                public void ValidateA() { }
                            }

                            public class UsageClass
                            {
                                [BusinessRule("RULE_A")]
                                public void MethodA() { }

                                [BusinessRule("RULE_B")]
                                public void MethodB() { }

                                [BusinessRule("RULE_C")]
                                public void MethodC() { }
                            }
                            """;

        var expected1 = CSharpAnalyzerVerifier<RequiresValidationAnalyzer>
            .Diagnostic("BR002")
            .WithArguments("RULE_B")
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(14, 6, 14, 28);


        var expected2 = CSharpAnalyzerVerifier<RequiresValidationAnalyzer>
            .Diagnostic("BR002")
            .WithArguments("RULE_C")
            .WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(17, 6, 17, 28);

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test, expected1, expected2);
    }

    [Test]
    public async Task ValidatorInDifferentClass_StillValid()
    {
        const string test = """
                            using MaVe.BusinessRules.Attributes;

                            public class ValidatorsA
                            {
                                [ImplementsBusinessRule("AUTH")]
                                public void ValidateAuth() { }
                            }

                            public class ValidatorsB
                            {
                                [ImplementsBusinessRule("ADMIN")]
                                public void ValidateAdmin() { }
                            }

                            public class UsageClass
                            {
                                [BusinessRule("AUTH")]
                                public void MethodA() { }

                                [BusinessRule("ADMIN")]
                                public void MethodB() { }
                            }
                            """;

        await CSharpAnalyzerVerifier<RequiresValidationAnalyzer>.VerifyAnalyzerAsync(test);
    }
}
