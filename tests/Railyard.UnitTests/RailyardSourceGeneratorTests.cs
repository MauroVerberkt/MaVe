using MaVe.Monads;
using MaVe.RailyardGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace MaVe.Railyard.UnitTests;

[TestFixture]
public class RailyardSourceGeneratorTests
{
    private static readonly MetadataReference[] _references = CreateReferences();

    [Test]
    public Task Generate_ForValidOperation_EmitsRegistrationAndYard()
    {
        const string source = """
                              using System.Threading;
                              using System.Threading.Tasks;
                              using MaVe.Monads;
                              using MaVe.Railyard;

                              [Operation("greet", Description = "Greets a user")]
                              public sealed class GreetOperation : Operation<GreetInput, GreetOutput>
                              {
                                  protected override Result<GreetInput> Validate(GreetInput input)
                                  {
                                      return string.IsNullOrWhiteSpace(input.Name)
                                          ? Result.Failure<GreetInput>(Error.Create("Name missing"))
                                          : Result.Success(input);
                                  }

                                  protected override Task<Result<GreetOutput>> ExecuteAsync(GreetInput input, CancellationToken ct)
                                  {
                                      return Task.FromResult(Result.Success(new GreetOutput($"Hello {input.Name}")));
                                  }
                              }

                              public sealed class GreetInput
                              {
                                  public string Name { get; set; } = string.Empty;
                              }

                              public sealed class GreetOutput
                              {
                                  public GreetOutput(string message)
                                  {
                                      Message = message;
                                  }

                                  public string Message { get; }
                              }
                              """;

        var driver = CreateDriver(source);
        var runResult = driver.GetRunResult();

        Assert.That(runResult.Diagnostics, Is.Empty);
        return Verify(driver);
    }

    [Test]
    public void Generate_ForDuplicateNames_ReportsRY1001()
    {
        const string source = """
                              using System.Threading;
                              using System.Threading.Tasks;
                              using MaVe.Monads;
                              using MaVe.Railyard;

                              [Operation("duplicate")]
                              public sealed class FirstOperation : Operation<FirstInput, FirstOutput>
                              {
                                  protected override Task<Result<FirstOutput>> ExecuteAsync(FirstInput input, CancellationToken ct)
                                  {
                                      return Task.FromResult(Result.Success(new FirstOutput()));
                                  }
                              }

                              [Operation("duplicate")]
                              public sealed class SecondOperation : Operation<SecondInput, SecondOutput>
                              {
                                  protected override Task<Result<SecondOutput>> ExecuteAsync(SecondInput input, CancellationToken ct)
                                  {
                                      return Task.FromResult(Result.Success(new SecondOutput()));
                                  }
                              }

                              public sealed class FirstInput { }
                              public sealed class FirstOutput { }
                              public sealed class SecondInput { }
                              public sealed class SecondOutput { }
                              """;

        var driver = CreateDriver(source);
        var diagnostics = driver.GetRunResult().Results.SelectMany(result => result.Diagnostics);

        Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "RY1001"), Is.True);
    }

    [Test]
    public void Generate_ForInvalidBase_ReportsRY1002()
    {
        const string source = """
                              using MaVe.Railyard;

                              [Operation("invalid")]
                              public sealed class InvalidOperation
                              {
                              }
                              """;

        var driver = CreateDriver(source);
        var diagnostics = driver.GetRunResult().Results.SelectMany(result => result.Diagnostics);

        Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "RY1002"), Is.True);
    }

    [Test]
    public void Generate_ForInvalidName_ReportsRY1003()
    {
        const string source = """
                              using System.Threading;
                              using System.Threading.Tasks;
                              using MaVe.Monads;
                              using MaVe.Railyard;

                              [Operation("bad name")]
                              public sealed class InvalidNameOperation : Operation<InvalidNameInput, InvalidNameOutput>
                              {
                                  protected override Task<Result<InvalidNameOutput>> ExecuteAsync(InvalidNameInput input, CancellationToken ct)
                                  {
                                      return Task.FromResult(Result.Success(new InvalidNameOutput()));
                                  }
                              }

                              public sealed class InvalidNameInput { }
                              public sealed class InvalidNameOutput { }
                              """;

        var driver = CreateDriver(source);
        var diagnostics = driver.GetRunResult().Results.SelectMany(result => result.Diagnostics);

        Assert.That(diagnostics.Any(diagnostic => diagnostic.Id == "RY1003"), Is.True);
    }

    [Test]
    public void Generate_WithSpecialCharactersInDescription_EscapesGeneratedStringLiterals()
    {
        const string source = """
                              using System.Threading;
                              using System.Threading.Tasks;
                              using MaVe.Monads;
                              using MaVe.Railyard;

                              [Operation("escape", Description = "Line1\nLine2\rTab\tQuote\"Slash\\Null\0Done")]
                              public sealed class EscapeOperation : Operation<EscapeInput, EscapeOutput>
                              {
                                  protected override Task<Result<EscapeOutput>> ExecuteAsync(EscapeInput input, CancellationToken ct)
                                  {
                                      return Task.FromResult(Result.Success(new EscapeOutput()));
                                  }
                              }

                              public sealed class EscapeInput { }
                              public sealed class EscapeOutput { }
                              """;

        var driver = CreateDriver(source);
        var runResult = driver.GetRunResult();
        var diagnostics = runResult.Results.SelectMany(result => result.Diagnostics).ToArray();
        var generatedSource = runResult.Results
            .SelectMany(result => result.GeneratedSources)
            .Single(generated => generated.HintName == "Railyard.Generated.g.cs")
            .SourceText
            .ToString();

        Assert.That(diagnostics, Is.Empty);
        Assert.That(generatedSource,
            Does.Contain("new OperationDescriptor(\"escape\", \"Line1\\nLine2\\rTab\\tQuote\\\"Slash\\\\Null\\0Done\")"));
    }

    private static GeneratorDriver CreateDriver(string source)
    {
        var compilation = CSharpCompilation.Create(
            "RailyardGeneratorTests",
            [CSharpSyntaxTree.ParseText(source)],
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new RailyardSourceGenerator());
        return driver.RunGenerators(compilation);
    }

    private static MetadataReference[] CreateReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available.");
        }

        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(Result).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(OperationAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location));

        return [.. references
            .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())];
    }
}
