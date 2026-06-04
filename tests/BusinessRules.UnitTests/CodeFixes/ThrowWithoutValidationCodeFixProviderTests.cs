using BusinessRules.UnitTests.TestHelpers;
using BusinessRules.UnitTests.Verifiers;
using BusinessRulesAnalyzer;
using BusinessRulesFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace BusinessRules.UnitTests.CodeFixes;

[TestFixture]
public class ThrowWithoutValidationCodeFixProviderTests
{
    [Test]
    public async Task CodeFix_AddsAttributeWithClassName_ToException()
    {
        const string source = """
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

        const string fixedSource = """
                                   using BusinessRules;
                                   using BusinessRules.Rules.Authentication;
                                   using BusinessRules.Attributes;

                                   public class TestClass
                                   {
                                       [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                       public void SomeMethod()
                                       {
                                           throw UserMustBeAuthenticated.ToException();
                                       }
                                   }
                                   """;

        var expected = CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("SomeMethod")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .VerifyCodeFixWithGeneratedCodeAsync(source, fixedSource, expected);
    }

    [Test]
    public async Task CodeFix_AddsAttributeWithClassName_DifferentRuleClass()
    {
        const string source = """
                              using BusinessRules;
                              using BusinessRules.Rules.Authorization;

                              public class TestClass
                              {
                                  public void ValidateAdmin()
                                  {
                                      {|#0:throw UserMustBeAdmin.ToException();|}
                                  }
                              }
                              """;

        const string fixedSource = """
                                   using BusinessRules;
                                   using BusinessRules.Rules.Authorization;
                                   using BusinessRules.Attributes;

                                   public class TestClass
                                   {
                                       [ImplementsBusinessRule(UserMustBeAdmin.Key)]
                                       public void ValidateAdmin()
                                       {
                                           throw UserMustBeAdmin.ToException();
                                       }
                                   }
                                   """;

        var expected = CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("ValidateAdmin")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .VerifyCodeFixWithGeneratedCodeAsync(source, fixedSource, expected);
    }

    [Test]
    public async Task CodeFix_AddsAttributeWithClassName_ToExceptionWithMessage()
    {
        const string source = """
                              using BusinessRules;
                              using BusinessRules.Rules.Authentication;

                              public class TestClass
                              {
                                  public void SomeMethod()
                                  {
                                      {|#0:throw UserMustBeAuthenticated.ToException("Custom message");|}
                                  }
                              }
                              """;

        const string fixedSource = """
                                   using BusinessRules;
                                   using BusinessRules.Rules.Authentication;
                                   using BusinessRules.Attributes;

                                   public class TestClass
                                   {
                                       [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                       public void SomeMethod()
                                       {
                                           throw UserMustBeAuthenticated.ToException("Custom message");
                                       }
                                   }
                                   """;

        var expected = CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("SomeMethod")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .VerifyCodeFixWithGeneratedCodeAsync(source, fixedSource, expected);
    }

    [Test]
    public async Task CodeFix_PreservesExistingAttributes()
    {
        const string source = """
                              using BusinessRules;
                              using BusinessRules.Rules.Authentication;
                              using System;

                              public class TestClass
                              {
                                  [Obsolete]
                                  public void SomeMethod()
                                  {
                                      {|#0:throw UserMustBeAuthenticated.ToException();|}
                                  }
                              }
                              """;

        const string fixedSource = """
                                   using BusinessRules;
                                   using BusinessRules.Rules.Authentication;
                                   using System;
                                   using BusinessRules.Attributes;

                                   public class TestClass
                                   {
                                       [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                       [Obsolete]
                                       public void SomeMethod()
                                       {
                                           throw UserMustBeAuthenticated.ToException();
                                       }
                                   }
                                   """;

        var expected = CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("SomeMethod")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .VerifyCodeFixWithGeneratedCodeAsync(source, fixedSource, expected);
    }

    [Test]
    public async Task CodeFix_DirectConstruction_NoFixOffered()
    {
        // When throwing via new BusinessRuleViolationException(variable),
        // the code fix cannot extract the rule key/class, so no fix is registered.
        const string source = """
                              using BusinessRules;
                              using BusinessRules.Rules.Authentication;

                              public class TestClass
                              {
                                  public void SomeMethod()
                                  {
                                      var rule = new UserMustBeAuthenticated();
                                      {|#0:throw new BusinessRuleViolationException(rule);|}
                                  }
                              }
                              """;

        var test = new CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>.Test
        {
            TestCode = source.Replace("\r\n", "\n"),
            FixedCode = source.Replace("\r\n", "\n"),
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck
        };
        test.TestState.Sources.Add(GeneratedBusinessRules.GetAllGeneratedSources());
        test.FixedState.Sources.Add(GeneratedBusinessRules.GetAllGeneratedSources());
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
                .Diagnostic("BR004")
                .WithLocation(0)
                .WithArguments("SomeMethod")
                .WithSeverity(DiagnosticSeverity.Warning));
        test.FixedState.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
                .Diagnostic("BR004")
                .WithLocation(0)
                .WithArguments("SomeMethod")
                .WithSeverity(DiagnosticSeverity.Warning));
        test.NumberOfIncrementalIterations = 0;
        test.NumberOfFixAllIterations = 0;

        await test.RunAsync();
    }

    [Test]
    public async Task CodeFix_MultipleThrowsInMethod_FixesFirst()
    {
        const string source = """
                              using BusinessRules;
                              using BusinessRules.Rules.Authentication;
                              using BusinessRules.Rules.Authorization;

                              public class TestClass
                              {
                                  public void SomeMethod(bool isAdmin)
                                  {
                                      {|#0:throw UserMustBeAuthenticated.ToException();|}
                                  }
                              }
                              """;

        const string fixedSource = """
                                   using BusinessRules;
                                   using BusinessRules.Rules.Authentication;
                                   using BusinessRules.Rules.Authorization;
                                   using BusinessRules.Attributes;

                                   public class TestClass
                                   {
                                       [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                       public void SomeMethod(bool isAdmin)
                                       {
                                           throw UserMustBeAuthenticated.ToException();
                                       }
                                   }
                                   """;

        var expected = CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .Diagnostic("BR004")
            .WithLocation(0)
            .WithArguments("SomeMethod")
            .WithSeverity(DiagnosticSeverity.Warning);

        await CSharpCodeFixVerifier<ThrowWithoutValidationAnalyzer, ThrowWithoutValidationCodeFixProvider>
            .VerifyCodeFixWithGeneratedCodeAsync(source, fixedSource, expected);
    }
}
