using HelperUnions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace HelperUnions.UnitTests.Verifiers;

public class LineEndingNormalizingVerifier : IVerifier
{
    private readonly IVerifier _inner = new DefaultVerifier();

    public void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        _inner.Empty(collectionName, collection);
    }

    public void Equal<T>(T expected, T actual, string? message = null)
    {
        if (expected is string expectedString && actual is string actualString)
        {
            var normalizedExpected = NormalizeLineEndings(expectedString);
            var normalizedActual = NormalizeLineEndings(actualString);

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
        if (message != null && IsLineEndingOnlyDiff(message))
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

    private static string NormalizeLineEndings(string input)
    {
        return input.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static bool IsLineEndingOnlyDiff(string message)
    {
        if (!message.Contains("Diff shown with expected as baseline:", StringComparison.Ordinal)
            || !message.Contains("<CR><LF>", StringComparison.Ordinal)
            || !message.Contains("<LF>", StringComparison.Ordinal))
        {
            return false;
        }

        var minusLines = new List<string>();
        var plusLines = new List<string>();

        foreach (var line in message.Split('\n'))
        {
            if (line.StartsWith("-", StringComparison.Ordinal))
            {
                minusLines.Add(line[1..]);
            }
            else if (line.StartsWith("+", StringComparison.Ordinal))
            {
                plusLines.Add(line[1..]);
            }
        }

        if (minusLines.Count == 0 || minusLines.Count != plusLines.Count)
        {
            return false;
        }

        for (var i = 0; i < minusLines.Count; i++)
        {
            var normalizedMinus = minusLines[i].Replace("<CR><LF>", "<LF>", StringComparison.Ordinal);
            var normalizedPlus = plusLines[i].Replace("<CR><LF>", "<LF>", StringComparison.Ordinal);

            if (!string.Equals(normalizedMinus, normalizedPlus, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
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

            var helperUnionsReference = MetadataReference.CreateFromFile(typeof(UnionAttribute).Assembly.Location);
            if (TestState.AdditionalReferences.All(reference => reference.Display != helperUnionsReference.Display))
            {
                TestState.AdditionalReferences.Add(helperUnionsReference);
            }
        }
    }

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>.Diagnostic(diagnosticId);

    public static async Task VerifyCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
