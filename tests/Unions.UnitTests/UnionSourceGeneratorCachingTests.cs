using MaVe.UnionsGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MaVe.Unions.UnitTests;

[TestFixture]
public class UnionSourceGeneratorCachingTests
{
    private const string SourceOutputStepName = "SourceOutput";

    private static GeneratorDriver CreateDriverWithTracking()
    {
        return CSharpGeneratorDriver.Create(
            [new UnionSourceGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                true));
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            "CachingTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(UnionAttribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<IncrementalStepRunReason> GetOutputReasons(GeneratorDriver driver)
    {
        return driver.GetRunResult().Results.Single()
            .TrackedOutputSteps[SourceOutputStepName]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason);
    }

    [Test]
    public void SameCompilation_SecondRun_OutputIsCached()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public partial record Payment
                              {
                                  public sealed record Card(string Number) : Payment;
                                  public sealed record Cash(decimal Amount) : Payment;
                              }
                              """;

        var compilation = CreateCompilation(source);

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation);

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Cached));
    }

    [Test]
    public void UnrelatedClassAdded_OutputIsCached()
    {
        const string source = """
                              using MaVe.Unions;

                              namespace Demo;

                              [Union]
                              public partial record Payment
                              {
                                  public sealed record Card(string Number) : Payment;
                                  public sealed record Cash(decimal Amount) : Payment;
                              }
                              """;

        var compilation = CreateCompilation(source);

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(compilation);

        var modifiedCompilation = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace Other { public class Unrelated { } }"));

        driver = driver.RunGenerators(modifiedCompilation);

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Cached));
    }

    [Test]
    public void CommentAddedInsideUnion_OutputIsCached()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    // This is a comment that should not affect caching
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Cached));
    }

    [Test]
    public void NamespaceChanged_OutputIsModified()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo.Modified;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Modified));
    }

    [Test]
    public void VariantAdded_OutputIsModified()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                    public sealed record Crypto(string Wallet) : Payment;
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Modified));
    }

    [Test]
    public void VariantRenamed_OutputIsModified()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record CreditCard(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Modified));
    }

    [Test]
    public void VariantParameterTypeChanged_OutputIsModified()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(int Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Modified));
    }

    [Test]
    public void VariantParameterNameChanged_OutputIsModified()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string Number) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo;

                                [Union]
                                public partial record Payment
                                {
                                    public sealed record Card(string CardNumber) : Payment;
                                    public sealed record Cash(decimal Amount) : Payment;
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Modified));
    }

    [Test]
    public void ContainingTypeChanged_OutputIsModified()
    {
        const string sourceV1 = """
                                using MaVe.Unions;

                                namespace Demo;

                                public partial class Container
                                {
                                    [Union]
                                    public partial record Payment
                                    {
                                        public sealed record Card(string Number) : Payment;
                                        public sealed record Cash(decimal Amount) : Payment;
                                    }
                                }
                                """;

        const string sourceV2 = """
                                using MaVe.Unions;

                                namespace Demo;

                                public partial class OtherContainer
                                {
                                    [Union]
                                    public partial record Payment
                                    {
                                        public sealed record Card(string Number) : Payment;
                                        public sealed record Cash(decimal Amount) : Payment;
                                    }
                                }
                                """;

        var driver = CreateDriverWithTracking();
        driver = driver.RunGenerators(CreateCompilation(sourceV1));
        driver = driver.RunGenerators(CreateCompilation(sourceV2));

        Assert.That(GetOutputReasons(driver), Is.All.EqualTo(IncrementalStepRunReason.Modified));
    }
}
