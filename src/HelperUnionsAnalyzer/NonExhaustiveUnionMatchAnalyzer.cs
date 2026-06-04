using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HelperUnionsAnalyzer;

/// <summary>
/// Reports DNHU0001 when a switch statement or switch expression on a union type does not cover all variants.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonExhaustiveUnionMatchAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "DNHU0001";
    private const string Category = "Usage";
    private const string UnionAttributeFullyQualifiedName = "HelperUnions.UnionAttribute";
    private const string MissingVariantsPropertyName = "MissingVariants";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Non-exhaustive union match",
        "Non-exhaustive union match. Missing variants: {0}.",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var unionAttributeSymbol =
                compilationContext.Compilation.GetTypeByMetadataName(UnionAttributeFullyQualifiedName);
            if (unionAttributeSymbol is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeSwitchStatement(syntaxContext, unionAttributeSymbol),
                SyntaxKind.SwitchStatement);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeSwitchExpression(syntaxContext, unionAttributeSymbol),
                SyntaxKind.SwitchExpression);
        });
    }

    private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context, INamedTypeSymbol unionAttributeSymbol)
    {
        if (context.Node is not SwitchStatementSyntax switchStatement)
        {
            return;
        }

        AnalyzeSwitchCore(
            context,
            switchStatement.Expression,
            switchStatement.SwitchKeyword.GetLocation(),
            switchStatement.Sections.SelectMany(section => section.Labels),
            unionAttributeSymbol,
            label => label switch
            {
                CasePatternSwitchLabelSyntax caseLabel => caseLabel.Pattern,
                _ => null
            },
            label => label is DefaultSwitchLabelSyntax);
    }

    private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context,
        INamedTypeSymbol unionAttributeSymbol)
    {
        if (context.Node is not SwitchExpressionSyntax switchExpression)
        {
            return;
        }

        AnalyzeSwitchCore(
            context,
            switchExpression.GoverningExpression,
            switchExpression.SwitchKeyword.GetLocation(),
            switchExpression.Arms,
            unionAttributeSymbol,
            arm => arm.Pattern,
            arm => IsDiscardOrVarPattern(arm.Pattern));
    }

    private static void AnalyzeSwitchCore<TNode>(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax switchExpression,
        Location location,
        IEnumerable<TNode> labelsOrArms,
        INamedTypeSymbol unionAttributeSymbol,
        Func<TNode, PatternSyntax?> getPattern,
        Func<TNode, bool> isDefaultOrDiscard)
    {
        var switchedType =
            context.SemanticModel.GetTypeInfo(switchExpression, context.CancellationToken).Type as INamedTypeSymbol;
        if (switchedType is null)
        {
            return;
        }

        if (!HasUnionAttribute(switchedType, unionAttributeSymbol))
        {
            return;
        }

        var orArms = labelsOrArms as TNode[] ?? labelsOrArms.ToArray();
        if (orArms.Any(isDefaultOrDiscard))
        {
            return;
        }

        var variants = switchedType
            .GetTypeMembers()
            .Where(typeMember =>
                typeMember.IsRecord &&
                typeMember.IsSealed &&
                SymbolEqualityComparer.Default.Equals(typeMember.BaseType, switchedType))
            .ToImmutableArray();

        if (variants.Length == 0)
        {
            return;
        }

        var covered = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var node in orArms)
        {
            var pattern = getPattern(node);
            if (pattern is null)
            {
                continue;
            }

            var matchedType = GetMatchedType(context.SemanticModel, pattern, context.CancellationToken);

            if (matchedType is not INamedTypeSymbol matchedNamedType)
            {
                continue;
            }

            if (variants.Any(variant => SymbolEqualityComparer.Default.Equals(variant, matchedNamedType)))
            {
                covered.Add(matchedNamedType);
            }
        }

        var missing = variants
            .Where(variant =>
                !covered.Any(coveredVariant => SymbolEqualityComparer.Default.Equals(coveredVariant, variant)))
            .Select(variant => variant.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        var properties =
            ImmutableDictionary<string, string?>.Empty.Add(MissingVariantsPropertyName, string.Join(",", missing));
        context.ReportDiagnostic(Diagnostic.Create(_rule, location, properties, string.Join(", ", missing)));
    }

    private static bool HasUnionAttribute(INamedTypeSymbol typeSymbol, INamedTypeSymbol unionAttributeSymbol)
    {
        return typeSymbol.GetAttributes()
            .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, unionAttributeSymbol));
    }

    private static ITypeSymbol? GetMatchedType(SemanticModel semanticModel, PatternSyntax pattern,
        CancellationToken cancellationToken)
    {
        return pattern switch
        {
            DeclarationPatternSyntax declarationPattern => semanticModel
                .GetTypeInfo(declarationPattern.Type, cancellationToken).Type,
            TypePatternSyntax typePattern => semanticModel.GetTypeInfo(typePattern.Type, cancellationToken).Type,
            ConstantPatternSyntax constantPattern
                when semanticModel.GetSymbolInfo(constantPattern.Expression, cancellationToken).Symbol is
                    INamedTypeSymbol namedTypeSymbol
                => namedTypeSymbol,
            RecursivePatternSyntax recursivePattern when recursivePattern.Type is not null
                => semanticModel.GetTypeInfo(recursivePattern.Type, cancellationToken).Type,
            _ => null
        };
    }

    private static bool IsDiscardOrVarPattern(PatternSyntax pattern)
    {
        return pattern switch
        {
            DiscardPatternSyntax => true,
            VarPatternSyntax => true,
            _ => false
        };
    }
}
