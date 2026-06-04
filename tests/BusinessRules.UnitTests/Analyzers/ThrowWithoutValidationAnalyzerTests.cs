using BusinessRules.UnitTests.Verifiers;
using BusinessRulesAnalyzer;
using Microsoft.CodeAnalysis;

namespace BusinessRules.UnitTests.Analyzers;

[TestFixture]
public class ThrowWithoutValidationAnalyzerTests
{
    [Test]
    public async Task ThrowBusinessRuleException_WithAttribute_NoDiagnostic()
    {
        const string test = """
                            using BusinessRules;
                            using BusinessRules.Attributes;
                            using BusinessRules.Rules.Authentication;

                            public class TestClass
                            {
                                [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                public void ValidateAuth()
                                {
                                    throw UserMustBeAuthenticated.ToException();
                                }
                            }
                            """;

        await CSharpAnalyzerVerifier<ThrowWithoutValidationAnalyzer>.VerifyAnalyzerWithGeneratedCodeAsync(test);
    }

    [Test]
    public async Task ThrowBusinessRuleException_WithoutAttribute_ReportsWarning()
    {
        const string test = """
                            using BusinessRules;
                            using BusinessRules.Rules.Authentication;

                            public class TestClass
                            {
                                public void SomeMethod()
                                {
                                    {|#0:throw UserMustBeAuthenticated.ToException();|}
                                }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<ThrowWithoutValidationAnalyzer>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("SomeMethod")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpAnalyzerVerifier<ThrowWithoutValidationAnalyzer>.VerifyAnalyzerWithGeneratedCodeAsync(test,
            expected);
    }

    [Test]
    public async Task ThrowRegularException_NoAttribute_NoDiagnostic()
    {
        const string test = """
                            using System;

                            public class TestClass
                            {
                                public void SomeMethod()
                                {
                                    throw new InvalidOperationException("Some error");
                                }
                            }
                            """;

        await CSharpAnalyzerVerifier<ThrowWithoutValidationAnalyzer>.VerifyAnalyzerAsync(test);
    }

    [Test]
    public async Task MultipleThrows_OnlyBusinessRuleFlagged()
    {
        const string test = """
                            using BusinessRules;
                            using BusinessRules.Attributes;
                            using BusinessRules.Rules.Authentication;
                            using System;

                            public class TestClass
                            {
                                [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                public void ValidMethod()
                                {
                                    throw UserMustBeAuthenticated.ToException();
                                }

                                public void InvalidMethod()
                                {
                                    {|#0:throw UserMustBeAuthenticated.ToException();|}
                                }

                                public void RegularMethod()
                                {
                                    throw new InvalidOperationException("This is fine");
                                }
                            }
                            """;

        var expected = CSharpAnalyzerVerifier<ThrowWithoutValidationAnalyzer>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("InvalidMethod")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpAnalyzerVerifier<ThrowWithoutValidationAnalyzer>.VerifyAnalyzerWithGeneratedCodeAsync(test,
            expected);
    }
}
