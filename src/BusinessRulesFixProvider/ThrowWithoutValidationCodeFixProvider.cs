using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MaVe.BusinessRulesFixProvider;

/// <summary>
/// Code fix provider for BR004: automatically adds an <c>[ImplementsBusinessRule]</c> attribute
/// to methods that throw business rule exceptions without one.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThrowWithoutValidationCodeFixProvider))]
[Shared]
public class ThrowWithoutValidationCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add [ImplementsBusinessRule] attribute";

    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ["BR004"];

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var throwNode = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .FirstOrDefault(node => node is ThrowStatementSyntax or ThrowExpressionSyntax);
        if (throwNode == null)
        {
            return;
        }

        var declarationNode = FindTargetDeclaration(throwNode);
        if (declarationNode == null)
        {
            return;
        }

        var semanticModel =
            await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        var thrownExpression = GetThrownExpression(throwNode);
        if (thrownExpression == null)
        {
            return;
        }

        var (ruleKey, className) = TryExtractRuleInfo(thrownExpression, semanticModel);

        // Only offer the fix if we can extract enough info to produce a valid attribute
        if (ruleKey == null && className == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                className != null
                    ? $"Add [ImplementsBusinessRule({className}.Key)]"
                    : $"Add [ImplementsBusinessRule(\"{ruleKey}\")]",
                c => AddValidatesAttributeAsync(context.Document, declarationNode, ruleKey, className, c),
                Title),
            diagnostic);
    }

    private static ExpressionSyntax? GetThrownExpression(SyntaxNode throwNode)
    {
        return throwNode switch
        {
            ThrowStatementSyntax throwStatement => throwStatement.Expression,
            ThrowExpressionSyntax throwExpression => throwExpression.Expression,
            _ => null
        };
    }

    private static SyntaxNode? FindTargetDeclaration(SyntaxNode throwNode)
    {
        for (var current = throwNode.Parent; current != null; current = current.Parent)
        {
            switch (current)
            {
                case MethodDeclarationSyntax:
                    return current;
                case ConstructorDeclarationSyntax constructorDeclaration:
                    return constructorDeclaration.Parent as ClassDeclarationSyntax;
            }
        }

        return null;
    }

    private static (string? ruleKey, string? className) TryExtractRuleInfo(ExpressionSyntax throwExpression,
        SemanticModel semanticModel)
    {
        // Handle: throw SomeRule.ToException() or throw SomeRule.ToFaultException()
        if (throwExpression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.Text: "ToException" or "ToFaultException"
            } memberAccess)
        {
            var typeSymbol = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            if (typeSymbol != null)
            {
                var keyField = typeSymbol.GetMembers("Key").OfType<IFieldSymbol>().FirstOrDefault();
                if (keyField?.ConstantValue is string key)
                {
                    return (key, typeSymbol.Name);
                }
            }
        }

        return (null, null);
    }

    private async Task<Document> AddValidatesAttributeAsync(
        Document document,
        SyntaxNode declarationNode,
        string? ruleKey,
        string? className,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var annotation = new SyntaxAnnotation("TargetDeclaration");
        root = root.ReplaceNode(declarationNode, declarationNode.WithAdditionalAnnotations(annotation));

        // Add using statement if not present
        if (root is CompilationUnitSyntax compilationUnit)
        {
            var hasUsing = compilationUnit.Usings.Any(u => u.Name?.ToString() == "MaVe.BusinessRules.Attributes");
            if (!hasUsing)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("MaVe.BusinessRules.Attributes"));
                root = compilationUnit.AddUsings(usingDirective);
            }
        }

        declarationNode = root.GetAnnotatedNodes(annotation).FirstOrDefault();
        if (declarationNode == null)
        {
            return document;
        }

        var attributeLists = GetAttributeLists(declarationNode);
        if (attributeLists == null)
        {
            return document;
        }

        // 1️⃣ Check if attribute already exists
        var alreadyHasAttribute = attributeLists.Value
            .SelectMany(a => a.Attributes)
            .Any(a =>
            {
                var name = a.Name.ToString();
                if (name != "ImplementsBusinessRule" && name != "ImplementsBusinessRuleAttribute")
                {
                    return false;
                }

                // Optional: Check for same argument
                if (className != null)
                {
                    return a.ArgumentList?.Arguments.Any(arg => arg.ToString().Contains($"{className}.Key")) == true;
                }

                if (ruleKey != null)
                {
                    return a.ArgumentList?.Arguments.Any(arg => arg.ToString().Contains(ruleKey)) == true;
                }

                return true; // attribute without args already exists
            });

        if (alreadyHasAttribute)
        {
            return document;
        }

        AttributeListSyntax attributeList;

        if (className != null)
        {
            // Generate: [ImplementsBusinessRule(ClassName.Key)]
            var memberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(className),
                SyntaxFactory.IdentifierName("Key"));

            attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName("ImplementsBusinessRule"),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.AttributeArgument(memberAccess))))));
        }
        else
        {
            // Generate: [ImplementsBusinessRule("RULE_KEY")]
            attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName("ImplementsBusinessRule"),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(ruleKey!))))))));
        }

        var leadingTrivia = declarationNode.GetLeadingTrivia();

        // Copy the end-of-line trivia from the class declaration to match existing style
        var classDecl = declarationNode.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        var eolTrivia =
            classDecl?.OpenBraceToken.TrailingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia))
            ?? SyntaxFactory.ElasticMarker;

        var attributeWithTrivia = attributeList
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(eolTrivia);

        var existingAttributes = attributeLists.Value.ToList();
        existingAttributes.Insert(0, attributeWithTrivia);

        var newDeclarationNode = WithAttributeLists(
            declarationNode,
            SyntaxFactory.List(existingAttributes));
        if (newDeclarationNode == null)
        {
            return document;
        }

        newDeclarationNode = newDeclarationNode
            .WithLeadingTrivia(SyntaxFactory.TriviaList())
            .WithoutAnnotations(annotation);

        var newRoot = root.ReplaceNode(declarationNode, newDeclarationNode);

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxList<AttributeListSyntax>? GetAttributeLists(SyntaxNode declarationNode)
    {
        return declarationNode switch
        {
            MethodDeclarationSyntax methodDeclaration => methodDeclaration.AttributeLists,
            ClassDeclarationSyntax classDeclaration => classDeclaration.AttributeLists,
            _ => null
        };
    }

    private static SyntaxNode? WithAttributeLists(SyntaxNode declarationNode,
        SyntaxList<AttributeListSyntax> attributeLists)
    {
        return declarationNode switch
        {
            MethodDeclarationSyntax methodDeclaration => methodDeclaration.WithAttributeLists(attributeLists),
            ClassDeclarationSyntax classDeclaration => classDeclaration.WithAttributeLists(attributeLists),
            _ => null
        };
    }
}
