using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MaVe.BusinessRulesAnalyzer;

/// <summary>
/// Analyzer (BR001): Reports an error when a business rule key used in an attribute
/// is not defined in the project's MaVe.BusinessRules.json file.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BusinessRuleKeyExistsAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "BR001";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Business rule key not found",
        "Business rule with key '{0}' is not defined in MaVe.BusinessRules.json",
        Category,
        DiagnosticSeverity.Error,
        true,
        "All business rule keys used in ImplementsBusinessRule or BusinessRule attributes must be defined in MaVe.BusinessRules.json.",
        customTags: []
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
            var requiresAttrSymbol =
                compilationContext.Compilation.GetTypeByMetadataName(
                    "MaVe.BusinessRules.Attributes.BusinessRuleAttribute");

            if (validatesAttrSymbol == null || requiresAttrSymbol == null)
            {
                return;
            }

            var definedRuleKeys = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
            var reportedKeys = new ConcurrentDictionary<(FileLinePositionSpan, string), byte>();

            // --- READ BUSINESS RULES FROM JSON FILE ---
            var jsonFile = compilationContext.Options.AdditionalFiles.FirstOrDefault(f =>
                f.Path.EndsWith("MaVe.BusinessRules.json", StringComparison.OrdinalIgnoreCase));
            if (jsonFile != null)
            {
                var jsonText = jsonFile.GetText(compilationContext.CancellationToken);
                if (jsonText != null)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(jsonText.ToString());
                        if (jsonDoc.RootElement.TryGetPropertyIgnoreCase("MaVe.BusinessRules", out var rulesArray) &&
                            rulesArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var rule in rulesArray.EnumerateArray())
                            {
                                if (rule.TryGetPropertyIgnoreCase("key", out var keyProp) &&
                                    keyProp.ValueKind == JsonValueKind.String)
                                {
                                    definedRuleKeys.TryAdd(keyProp.GetString()!, 0);
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Invalid JSON - skip
                    }
                }
            }

            // --- COLLECT ATTRIBUTES ---
            compilationContext.RegisterSyntaxNodeAction(nodeContext =>
            {
                if (nodeContext.Node is not AttributeSyntax attribute)
                {
                    return;
                }

                if (nodeContext.SemanticModel.GetTypeInfo(attribute).Type is not INamedTypeSymbol attrType)
                {
                    return;
                }

                if (!SymbolEqualityComparer.Default.Equals(attrType, validatesAttrSymbol) &&
                    !SymbolEqualityComparer.Default.Equals(attrType, requiresAttrSymbol))
                {
                    return;
                }

                var firstArg = attribute.ArgumentList?.Arguments.FirstOrDefault();
                if (firstArg == null)
                {
                    return;
                }

                // Try to get the constant value (works for both literals and constants like UserMustBeAuthenticated.Key)
                var constantValue = nodeContext.SemanticModel.GetConstantValue(firstArg.Expression);
                if (!constantValue.HasValue || constantValue.Value is not string ruleKey)
                {
                    return;
                }

                var locationSpan = firstArg.Expression.GetLocation().GetLineSpan();
                if (!definedRuleKeys.ContainsKey(ruleKey) &&
                    reportedKeys.TryAdd((locationSpan, ruleKey), 0))
                {
                    nodeContext.ReportDiagnostic(Diagnostic.Create(_rule, firstArg.Expression.GetLocation(), ruleKey));
                }
            }, SyntaxKind.Attribute);
        });
    }
}
