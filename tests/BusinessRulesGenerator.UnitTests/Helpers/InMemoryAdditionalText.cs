using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MaVe.BusinessRulesGenerator.UnitTests.Helpers;

internal sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
{
    private readonly SourceText _text = SourceText.From(text);

    public override string Path { get; } = path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return _text;
    }
}
