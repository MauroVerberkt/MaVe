using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MaVe.BusinessRulesAnalyzer;

/// <summary>
/// Analyzer (BR004): Warns when a method throws a business rule exception
/// without having the <c>[ImplementsBusinessRule]</c> attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ThrowWithoutValidationAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "BR004";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Throwing BusinessRule without validation",
        "Throwing a BusinessRule exception without [ImplementsBusinessRule] attribute on method '{0}'",
        Category,
        DiagnosticSeverity.Warning,
        true,
        "Methods that throw BusinessRule exceptions should have the [ImplementsBusinessRule] attribute to document what rule is being validated."
    );

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var validatesAttrSymbol =
                compilationContext.Compilation.GetTypeByMetadataName(
                    "MaVe.BusinessRules.Attributes.ImplementsBusinessRuleAttribute");
            var businessRuleFaultSymbol =
                compilationContext.Compilation.GetTypeByMetadataName("MaVe.BusinessRules.BusinessRuleFault");
            var businessRuleViolationSymbol =
                compilationContext.Compilation.GetTypeByMetadataName("MaVe.BusinessRules.BusinessRuleViolationException");

            if (validatesAttrSymbol == null || (businessRuleFaultSymbol == null && businessRuleViolationSymbol == null))
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(nodeContext =>
            {
                var thrownExpression = nodeContext.Node switch
                {
                    ThrowStatementSyntax throwStatement => throwStatement.Expression,
                    ThrowExpressionSyntax throwExpression => throwExpression.Expression,
                    _ => null
                };

                // Check if we're throwing a BusinessRuleFault or derived type
                if (thrownExpression == null)
                {
                    return;
                }

                var typeInfo = nodeContext.SemanticModel.GetTypeInfo(thrownExpression);
                if (typeInfo.Type == null)
                {
                    return;
                }

                // Check if the thrown type is BusinessRuleFault, BusinessRuleViolationException, or FaultException<BusinessRuleFault>
                var isBusinessRuleException = false;
                var currentType = typeInfo.Type;

                while (currentType != null)
                {
                    if ((businessRuleFaultSymbol != null &&
                         SymbolEqualityComparer.Default.Equals(currentType, businessRuleFaultSymbol)) ||
                        (businessRuleViolationSymbol != null &&
                         SymbolEqualityComparer.Default.Equals(currentType, businessRuleViolationSymbol)))
                    {
                        isBusinessRuleException = true;
                        break;
                    }

                    // Check for FaultException<BusinessRuleFault>
                    if (currentType is INamedTypeSymbol namedType && namedType.IsGenericType)
                    {
                        var typeArgs = namedType.TypeArguments;
                        if (typeArgs.Length > 0 && businessRuleFaultSymbol != null &&
                            SymbolEqualityComparer.Default.Equals(typeArgs[0], businessRuleFaultSymbol))
                        {
                            isBusinessRuleException = true;
                            break;
                        }
                    }

                    currentType = currentType.BaseType;
                }

                if (!isBusinessRuleException)
                {
                    return;
                }

                var containingSymbol = GetContainingSymbol(nodeContext.Node, nodeContext.SemanticModel);
                if (containingSymbol == null)
                {
                    return;
                }

                // Check if symbol or containing type has ImplementsBusinessRule attribute
                var hasValidatesAttribute = HasImplementsBusinessRuleAttribute(containingSymbol, validatesAttrSymbol) ||
                                            HasImplementsBusinessRuleAttribute((containingSymbol as IMethodSymbol)
                                                ?.AssociatedSymbol, validatesAttrSymbol) ||
                                            HasImplementsBusinessRuleAttribute(containingSymbol.ContainingType,
                                                validatesAttrSymbol);

                if (!hasValidatesAttribute)
                {
                    nodeContext.ReportDiagnostic(Diagnostic.Create(
                        _rule,
                        nodeContext.Node.GetLocation(),
                        GetDisplayName(containingSymbol)));
                }
            }, SyntaxKind.ThrowStatement, SyntaxKind.ThrowExpression);
        });
    }

    private static ISymbol? GetContainingSymbol(SyntaxNode node, SemanticModel semanticModel)
    {
        for (var current = node; current != null; current = current.Parent)
        {
            switch (current)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    return semanticModel.GetDeclaredSymbol(methodDeclaration);
                case ConstructorDeclarationSyntax constructorDeclaration:
                    return semanticModel.GetDeclaredSymbol(constructorDeclaration);
                case AccessorDeclarationSyntax accessorDeclaration:
                    return semanticModel.GetDeclaredSymbol(accessorDeclaration);
                case LocalFunctionStatementSyntax localFunctionDeclaration:
                    return semanticModel.GetDeclaredSymbol(localFunctionDeclaration);
            }
        }

        return null;
    }

    private static bool HasImplementsBusinessRuleAttribute(ISymbol? symbol, INamedTypeSymbol validatesAttrSymbol)
    {
        if (symbol == null)
        {
            return false;
        }

        return symbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, validatesAttrSymbol));
    }

    private static string GetDisplayName(ISymbol symbol)
    {
        if (symbol is not IMethodSymbol methodSymbol)
        {
            return symbol.Name;
        }

        return methodSymbol.MethodKind switch
        {
            MethodKind.Constructor => methodSymbol.ContainingType.Name,
            MethodKind.PropertyGet or MethodKind.PropertySet => methodSymbol.AssociatedSymbol?.Name ?? methodSymbol.Name,
            _ => methodSymbol.Name
        };
    }
}
