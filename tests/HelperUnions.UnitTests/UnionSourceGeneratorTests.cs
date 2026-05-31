using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HelperUnions.UnitTests;

[TestFixture]
public class UnionSourceGeneratorTests
{
    private static GeneratorDriver CreateDriver()
    {
        var generator = new HelperUnionsGenerator.UnionSourceGenerator();
        return CSharpGeneratorDriver.Create(generator);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            assemblyName: "HelperUnions.Tests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(UnionAttribute).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Test]
    public Task ValidUnionWithInheritingVariants_GeneratesAbstractBase()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;

                public sealed record Supplier(string CompanyName, int Rating) : BusinessParty;

                public sealed record Prospect() : BusinessParty;
            }
            """;

        var driver = CreateDriver()
            .RunGenerators(CreateCompilation(source));

        return Verify(driver);
    }

    [Test]
    public void UnionWithoutInheritingVariants_GeneratesNothing()
    {
        const string source = """
            using HelperUnions;

            [Union]
            public partial record BusinessParty
            {
                public sealed record Customer(string Name);
            }
            """;

        var result = CreateDriver()
            .RunGenerators(CreateCompilation(source))
            .GetRunResult();

        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [Test]
    public void UnionOnNonPartialRecord_GeneratesNothing()
    {
        const string source = """
            using HelperUnions;

            [Union]
            public record BusinessParty
            {
                public sealed record Customer(string Name) : BusinessParty;
            }
            """;

        var result = CreateDriver()
            .RunGenerators(CreateCompilation(source))
            .GetRunResult();

        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [Test]
    public void FirstZeroPayloadVariant_DoesNotGenerateValueOverload()
    {
        const string source = """
            using HelperUnions;

            namespace Demo;

            [Union]
            public partial record Outcome
            {
                public sealed record Empty() : Outcome;

                public sealed record Data(string Value) : Outcome;
            }
            """;

        var result = CreateDriver()
            .RunGenerators(CreateCompilation(source))
            .GetRunResult();

        var generatedSource = result.GeneratedTrees.Single().ToString();

        Assert.That(generatedSource, Does.Contain("public __MatchBuilder_1<TResult> Empty<TResult>(global::System.Func<TResult> handler)"));
        Assert.That(generatedSource, Does.Not.Contain("public __MatchBuilder_1<TResult> Empty<TResult>(TResult value)"));
    }
}
