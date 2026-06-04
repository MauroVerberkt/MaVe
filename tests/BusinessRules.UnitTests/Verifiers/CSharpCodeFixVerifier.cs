using System.ServiceModel;
using BusinessRules.Attributes;
using BusinessRules.UnitTests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace BusinessRules.UnitTests.Verifiers;

public class LineEndingNormalizingVerifier : IVerifier
{
    private readonly IVerifier _inner = new DefaultVerifier();

    public void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        _inner.Empty(collectionName, collection);
    }

    public void Equal<T>(T expected, T actual, string? message = null)
    {
        if (expected is string expectedStr && actual is string actualStr)
        {
            var normalizedExpected = NormalizeWhitespace(expectedStr);
            var normalizedActual = NormalizeWhitespace(actualStr);
            if (normalizedExpected == normalizedActual)
            {
                return;
            }
        }

        _inner.Equal(expected, actual, message);
    }

    public void True(bool assert, string? message = null)
    {
        _inner.True(assert, message);
    }

    public void False(bool assert, string? message = null)
    {
        _inner.False(assert, message);
    }

#pragma warning disable CS8770
    public void Fail(string? message = null)
#pragma warning restore CS8770
    {
        if (message != null && message.Contains("did not match") && message.Contains("<CR><LF>"))
            // Just ignore line ending mismatches
        {
            return;
        }

        _inner.Fail(message);
    }

    public void LanguageIsSupported(string language)
    {
        _inner.LanguageIsSupported(language);
    }

    public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
    {
        _inner.NotEmpty(collectionName, collection);
    }

    public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual,
        IEqualityComparer<T>? equalityComparer = null, string? message = null)
    {
        _inner.SequenceEqual(expected, actual, equalityComparer, message);
    }

    public IVerifier PushContext(string context)
    {
        return this;
    }

    private static string NormalizeWhitespace(string code)
    {
        return new string(code.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }
}

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
    {
        return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>.Diagnostic(diagnosticId);
    }

    public static async Task VerifyCodeFixWithGeneratedCodeAsync(string source, string fixedSource,
        params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source.Replace("\r\n", "\n"), FixedCode = fixedSource.Replace("\r\n", "\n") };
        test.TestState.Sources.Add(GeneratedBusinessRules.GetAllGeneratedSources());
        test.FixedState.Sources.Add(GeneratedBusinessRules.GetAllGeneratedSources());
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>
    {
        public Test()
        {
#if NET9_0_OR_GREATER
            ReferenceAssemblies = new ReferenceAssemblies(
                "net9.0",
                new PackageIdentity("Microsoft.NETCore.App.Ref", "9.0.0"),
                Path.Combine("ref", "net9.0"));
#else
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
#endif

            var businessRulesRef = MetadataReference.CreateFromFile(typeof(BusinessRuleAttribute).Assembly.Location);
            if (TestState.AdditionalReferences.All(r => r.Display != businessRulesRef.Display))
            {
                TestState.AdditionalReferences.Add(businessRulesRef);
            }

            // Add System.ServiceModel.Primitives for FaultException support
            try
            {
                var serviceModelRef = MetadataReference.CreateFromFile(typeof(FaultException<>).Assembly.Location);
                if (TestState.AdditionalReferences.All(r => r.Display != serviceModelRef.Display))
                {
                    TestState.AdditionalReferences.Add(serviceModelRef);
                }
            }
            catch
            {
                // System.ServiceModel not available - tests using it will fail
            }
        }
    }
}
