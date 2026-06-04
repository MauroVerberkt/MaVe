using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BusinessRulesAnalyzer;

/// <summary>
/// Analyzer (BR002/BR003): Ensures every <c>[BusinessRule]</c> attribute has a corresponding
/// <c>[ImplementsBusinessRule]</c> somewhere in the compilation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiresValidationAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticIdError = "BR002";
    private const string DiagnosticIdWarning = "BR003";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor _errorRule = new(
        DiagnosticIdError,
        "Missing ImplementsBusinessRule attribute",
        "Business rule '{0}' is required but not validated anywhere in the compilation",
        Category,
        DiagnosticSeverity.Error,
        true,
        "When BusinessRule has enforceValidation=true, a ImplementsBusinessRule with the same ruleKey must exist in the compilation.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
    );

    private static readonly DiagnosticDescriptor _warningRule = new(
        DiagnosticIdWarning,
        "Missing ImplementsBusinessRule attribute",
        "Business rule '{0}' is required but not validated anywhere in the compilation",
        Category,
        DiagnosticSeverity.Warning,
        true,
        "When BusinessRule has enforceValidation=false, a ImplementsBusinessRule with the same ruleKey should exist in the compilation.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
    );

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_errorRule, _warningRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var requiresAttrSymbol =
                compilationContext.Compilation.GetTypeByMetadataName(
                    "BusinessRules.Attributes.BusinessRuleAttribute");
            var validatesAttrSymbol =
                compilationContext.Compilation.GetTypeByMetadataName(
                    "BusinessRules.Attributes.ImplementsBusinessRuleAttribute");

            if (requiresAttrSymbol == null || validatesAttrSymbol == null)
            {
                return;
            }

            var validatedKeys = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
            var requiredRules = new ConcurrentBag<(string RuleKey, bool EnforceValidation, Location Location)>();
            var reportedKeys = new ConcurrentDictionary<(FileLinePositionSpan, string), byte>();

            // --- PRE-SCAN ALL ImplementsBusinessRule ATTRIBUTES ---
            // Must scan synchronously before other actions to avoid race conditions.
            // Suppressing RS1030: GetSemanticModel is intentional here for correct ordering.
#pragma warning disable RS1030
            foreach (var tree in compilationContext.Compilation.SyntaxTrees)
            {
                var model = compilationContext.Compilation.GetSemanticModel(tree);
                var attributes = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>();

                foreach (var attr in attributes)
                {
                    var type = model.GetTypeInfo(attr).Type as INamedTypeSymbol;
                    if (type == null || !SymbolEqualityComparer.Default.Equals(type, validatesAttrSymbol))
                    {
                        continue;
                    }

                    var firstArg = attr.ArgumentList?.Arguments.FirstOrDefault();
                    if (firstArg != null)
                    {
                        var constantValue = model.GetConstantValue(firstArg.Expression);
                        if (constantValue.HasValue && constantValue.Value is string ruleKey)
                        {
                            validatedKeys.TryAdd(ruleKey, 0);
                        }
                    }
                }
            }
#pragma warning restore RS1030

            // --- COLLECT REQUIRED RULES ---
            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var symbol = symbolContext.Symbol;
                foreach (var attr in symbol.GetAttributes().Where(attr =>
                             SymbolEqualityComparer.Default.Equals(attr.AttributeClass, requiresAttrSymbol)))
                {
                    if (attr.ConstructorArguments.Length == 0 ||
                        !(attr.ConstructorArguments[0].Value is string ruleKey))
                    {
                        continue;
                    }

                    var enforceValidation = true;
                    if (attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is bool enforce)
                    {
                        enforceValidation = enforce;
                    }
                    else
                    {
                        var namedArg = attr.NamedArguments.FirstOrDefault(kvp =>
                            kvp.Key == "enforceValidation" || kvp.Key == "EnforceValidation");
                        if (namedArg.Key != null && namedArg.Value.Value is bool namedEnforce)
                        {
                            enforceValidation = namedEnforce;
                        }
                    }

                    var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                                   symbol.Locations.FirstOrDefault();
                    if (location == null)
                    {
                        continue;
                    }

                    requiredRules.Add((ruleKey, enforceValidation, location));

                    // Partial/live diagnostic
                    var locationSpan = location.GetLineSpan();
                    if (!validatedKeys.ContainsKey(ruleKey) &&
                        reportedKeys.TryAdd((locationSpan, ruleKey), 0))
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(
                            enforceValidation ? _errorRule : _warningRule,
                            location,
                            ruleKey));
                    }
                }
            }, SymbolKind.Method, SymbolKind.NamedType);

            // --- FULL COMPILATION-END DIAGNOSTICS ---
            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (var (ruleKey, enforceValidation, location) in requiredRules)
                {
                    var locationSpan = location.GetLineSpan();
                    if (!validatedKeys.ContainsKey(ruleKey) &&
                        reportedKeys.TryAdd((locationSpan, ruleKey), 0))
                    {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            enforceValidation ? _errorRule : _warningRule,
                            location,
                            ruleKey));
                    }
                }
            });
        });
    }
}
