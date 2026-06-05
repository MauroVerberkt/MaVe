using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace MaVe.UnionsFixProvider;

/// <summary>
/// Adds missing union variants for DNHU0001 diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonExhaustiveUnionMatchCodeFixProvider))]
[Shared]
public sealed class NonExhaustiveUnionMatchCodeFixProvider : CodeFixProvider
{
    private const string DiagnosticId = "DNHU0001";
    private const string MissingVariantsPropertyName = "MissingVariants";
    private const string Title = "Add missing union variant arms";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
        {
            return;
        }

        if (!diagnostic.Properties.TryGetValue(MissingVariantsPropertyName, out var missingVariantsValue)
            || string.IsNullOrWhiteSpace(missingVariantsValue))
        {
            return;
        }

        var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
        var switchNode = token.Parent?.AncestorsAndSelf().FirstOrDefault(node =>
            node is SwitchStatementSyntax or SwitchExpressionSyntax);

        if (switchNode is null)
        {
            return;
        }

        var missingVariants = missingVariantsValue!
            .Split(',')
            .Select(variantName => variantName.Trim())
            .Where(variantName => variantName.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (missingVariants.Length == 0)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => AddMissingArmsAsync(
                    context.Document,
                    diagnostic.Location.SourceSpan,
                    missingVariants,
                    cancellationToken),
                Title),
            diagnostic);
    }

    private static async Task<Document> AddMissingArmsAsync(
        Document document,
        TextSpan diagnosticSpan,
        string[] missingVariants,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        var token = root.FindToken(diagnosticSpan.Start);
        var switchNode = token.Parent?.AncestorsAndSelf().FirstOrDefault(node =>
            node is SwitchStatementSyntax or SwitchExpressionSyntax);

        if (switchNode is null)
        {
            return document;
        }

        var switchedExpression = switchNode switch
        {
            SwitchStatementSyntax switchStatement => switchStatement.Expression,
            SwitchExpressionSyntax switchExpression => switchExpression.GoverningExpression,
            _ => null
        };

        if (switchedExpression is null)
        {
            return document;
        }

        var switchedType = semanticModel.GetTypeInfo(switchedExpression, cancellationToken).Type as INamedTypeSymbol;
        if (switchedType is null)
        {
            return document;
        }

        var unionTypeName = switchedType.ToMinimalDisplayString(semanticModel, switchNode.SpanStart);

        var updatedSwitchNode = switchNode switch
        {
            SwitchStatementSyntax switchStatement =>
                AddMissingSections(switchStatement, missingVariants, unionTypeName),
            SwitchExpressionSyntax switchExpression => AddMissingExpressionArms(switchExpression, missingVariants,
                unionTypeName),
            _ => switchNode
        };

        var updatedRoot =
            root.ReplaceNode(switchNode, updatedSwitchNode.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static SwitchExpressionSyntax AddMissingExpressionArms(
        SwitchExpressionSyntax switchExpression,
        IEnumerable<string> missingVariants,
        string unionTypeName)
    {
        var arms = switchExpression.Arms;
        foreach (var missingVariant in missingVariants)
        {
            var pattern = SyntaxFactory.DeclarationPattern(
                SyntaxFactory.ParseTypeName($"{unionTypeName}.{missingVariant}"),
                SyntaxFactory.DiscardDesignation());

            var throwExpression = SyntaxFactory.ThrowExpression(CreateNotImplementedExceptionExpression());
            var arm = SyntaxFactory.SwitchExpressionArm(pattern, throwExpression);
            arms = arms.Add(arm);
        }

        return switchExpression.WithArms(arms);
    }

    private static SwitchStatementSyntax AddMissingSections(
        SwitchStatementSyntax switchStatement,
        IEnumerable<string> missingVariants,
        string unionTypeName)
    {
        var sections = switchStatement.Sections;
        foreach (var missingVariant in missingVariants)
        {
            var pattern = SyntaxFactory.DeclarationPattern(
                SyntaxFactory.ParseTypeName($"{unionTypeName}.{missingVariant}"),
                SyntaxFactory.DiscardDesignation());

            var label = SyntaxFactory.CasePatternSwitchLabel(
                pattern,
                null,
                SyntaxFactory.Token(SyntaxKind.ColonToken));

            var throwStatement = SyntaxFactory.ThrowStatement(CreateNotImplementedExceptionExpression());

            var section = SyntaxFactory.SwitchSection(
                [label],
                [throwStatement]);

            sections = sections.Add(section);
        }

        return switchStatement.WithSections(sections);
    }

    private static ObjectCreationExpressionSyntax CreateNotImplementedExceptionExpression()
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.NotImplementedException"))
            .WithArgumentList(SyntaxFactory.ArgumentList());
    }
}
