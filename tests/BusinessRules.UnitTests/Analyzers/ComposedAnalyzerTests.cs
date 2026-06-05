using System.Collections.Immutable;
using System.Reflection;
using System.ServiceModel;
using MaVe.BusinessRulesAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MaVe.BusinessRules.UnitTests.Analyzers;

[TestFixture]
public class ComposedAnalyzerTests
{
    private const string TestMaVeBusinessRulesJson = """
                                                 {
                                                   "MaVe.BusinessRules": [
                                                     {
                                                       "className": "UserMustBeAuthenticated",
                                                       "key": "USER_AUTH",
                                                       "requirement": "User must be authenticated",
                                                       "description": "User must provide valid authentication credentials",
                                                       "category": "Authentication"
                                                     },
                                                     {
                                                       "className": "UserMustBeAdmin",
                                                       "key": "USER_ADMIN",
                                                       "requirement": "User must be admin",
                                                       "description": "User must have admin role",
                                                       "category": "Authorization"
                                                     },
                                                     {
                                                       "className": "AgeMinimum",
                                                       "key": "AGE_MIN",
                                                       "requirement": "Age minimum",
                                                       "description": "User must meet minimum age requirement",
                                                       "category": "Validation"
                                                     },
                                                     {
                                                       "className": "EmailVerified",
                                                       "key": "EMAIL_VERIFIED",
                                                       "requirement": "Email verified",
                                                       "description": "User email must be verified",
                                                       "category": "Validation"
                                                     },
                                                     {
                                                       "className": "TermsAccepted",
                                                       "key": "TERMS_ACCEPTED",
                                                       "requirement": "Terms accepted",
                                                       "description": "User must accept terms of service",
                                                       "category": "General"
                                                     },
                                                     {
                                                       "className": "SessionActive",
                                                       "key": "SESSION_ACTIVE",
                                                       "requirement": "Session active",
                                                       "description": "User session must be active",
                                                       "category": "General"
                                                     }
                                                   ]
                                                 }
                                                 """;

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(BusinessRule<>).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
        };

        try
        {
            references.Add(MetadataReference.CreateFromFile(typeof(FaultException<>).Assembly.Location));
        }
        catch
        {
            // ServiceModel not available in this context
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source,
        params DiagnosticAnalyzer[] analyzers)
    {
        var compilation = CreateCompilation(source);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [..analyzers],
            new AnalyzerOptions(
            [
                new InMemoryAdditionalText("Test.MaVe.BusinessRules.json", TestMaVeBusinessRulesJson)
            ]));

        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
        return [..diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning)];
    }

    [Test]
    public async Task AllAnalyzers_ValidCode_NoDiagnostics()
    {
        const string source = """
                              using MaVe.BusinessRules.Attributes;

                              public class GlobalValidators
                              {
                                  [ImplementsBusinessRule("USER_AUTH")]
                                  public void ValidateUserAuth() { }

                                  [ImplementsBusinessRule("USER_ADMIN")]
                                  public void ValidateUserAdmin() { }
                              }

                              public class ValidCases
                              {
                                  [BusinessRule("USER_AUTH")]
                                  public void ValidMethod() { }

                                  [BusinessRule("USER_ADMIN")]
                                  public void AnotherValidMethod() { }
                              }
                              """;

        var diagnostics = await GetDiagnosticsAsync(
            source,
            new BusinessRuleKeyExistsAnalyzer(),
            new RequiresValidationAnalyzer(),
            new ThrowWithoutValidationAnalyzer());

        Assert.That(diagnostics, Is.Empty,
            $"Expected no diagnostics but got: {string.Join(", ", diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"))}");
    }

    [Test]
    public async Task AllAnalyzers_FullPipeline_ReportsExpectedDiagnostics()
    {
        const string source = """
                              using MaVe.BusinessRules;
                              using MaVe.BusinessRules.Attributes;
                              using MaVe.BusinessRules.Rules.Authentication;

                              public class Validators
                              {
                                  [ImplementsBusinessRule(UserMustBeAuthenticated.Key)]
                                  public void ValidateAuth()
                                  {
                                      throw UserMustBeAuthenticated.ToException();
                                  }

                                  [ImplementsBusinessRule("INVALID_RULE")]
                                  public void ValidateInvalid() { }
                              }

                              public class Usage
                              {
                                  [BusinessRule(UserMustBeAuthenticated.Key)]
                                  public void GoodMethod() { }

                                  [BusinessRule("MISSING_VALIDATOR")]
                                  public void BadMethod() { }

                                  public void ThrowWithoutAttr()
                                  {
                                      throw UserMustBeAuthenticated.ToException();
                                  }
                              }
                              """;

        var diagnostics = await GetDiagnosticsAsync(
            source,
            new BusinessRuleKeyExistsAnalyzer(),
            new RequiresValidationAnalyzer(),
            new ThrowWithoutValidationAnalyzer());

        var br001 = diagnostics.Where(d => d.Id == "BR001").ToList();
        var br002 = diagnostics.Where(d => d.Id == "BR002").ToList();

        // BR001: Both INVALID_RULE and MISSING_VALIDATOR are not in JSON
        Assert.That(br001, Has.Count.EqualTo(2),
            $"Should have BR001 for INVALID_RULE and MISSING_VALIDATOR. Got: {string.Join(", ", br001.Select(d => d.GetMessage()))}");
        // BR002: MISSING_VALIDATOR has no ImplementsBusinessRule
        Assert.That(br002, Has.Count.EqualTo(1),
            $"Should have BR002 for MISSING_VALIDATOR. Got: {string.Join(", ", br002.Select(d => d.GetMessage()))}");
    }

    [Test]
    public async Task BR001_And_BR002_Combined_InvalidKey_And_MissingValidator()
    {
        const string source = """
                              using MaVe.BusinessRules.Attributes;

                              public class TestClass
                              {
                                  [BusinessRule("INVALID_KEY")]
                                  public void TestMethod() { }
                              }
                              """;

        var diagnostics = await GetDiagnosticsAsync(
            source,
            new BusinessRuleKeyExistsAnalyzer(),
            new RequiresValidationAnalyzer());

        Assert.That(diagnostics.Any(d => d.Id == "BR001"), Is.True, "Should report BR001 for invalid key");
        Assert.That(diagnostics.First(d => d.Id == "BR001").GetMessage(), Does.Contain("INVALID_KEY"));
    }

    [Test]
    public async Task BR003_EnforceFalse_ReportsWarning()
    {
        const string source = """
                              using MaVe.BusinessRules.Attributes;

                              public class TestClass
                              {
                                  [BusinessRule("EMAIL_VERIFIED", enforceValidation: false)]
                                  public void TestMethod() { }
                              }
                              """;

        var diagnostics = await GetDiagnosticsAsync(source, new RequiresValidationAnalyzer());

        Assert.That(diagnostics.Count(d => d.Id == "BR003"), Is.EqualTo(1));
        Assert.That(diagnostics.First(d => d.Id == "BR003").Severity, Is.EqualTo(DiagnosticSeverity.Warning));
    }

    [Test]
    public async Task BR004_ThrowWithoutAttribute_ReportsWarning()
    {
        const string source = """
                              using MaVe.BusinessRules;

                              public class TestClass
                              {
                                  public void TestMethod()
                                  {
                                      throw new BusinessRuleViolationException("AUTH", "Error");
                                  }
                              }
                              """;

        var diagnostics = await GetDiagnosticsAsync(source, new ThrowWithoutValidationAnalyzer());

        Assert.That(diagnostics.Count(d => d.Id == "BR004"), Is.EqualTo(1));
        Assert.That(diagnostics.First(d => d.Id == "BR004").Severity, Is.EqualTo(DiagnosticSeverity.Warning));
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText _text = SourceText.From(text);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }
    }
}
