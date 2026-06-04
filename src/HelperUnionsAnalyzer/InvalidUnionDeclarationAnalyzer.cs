using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HelperUnionsAnalyzer;

/// <summary>
/// Reports DNHU0003 when <c>[Union]</c> is applied to anything other than a partial record declaration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidUnionDeclarationAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "DNHU0003";
    private const string Category = "Usage";
    private const string UnionAttributeFullyQualifiedName = "HelperUnions.UnionAttribute";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Invalid union declaration",
        "[Union] may only be applied to partial record declarations",
        Category,
        DiagnosticSeverity.Error,
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

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                if (symbolContext.Symbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return;
                }

                var unionAttribute = namedTypeSymbol
                    .GetAttributes()
                    .FirstOrDefault(attribute =>
                        SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, unionAttributeSymbol));

                if (unionAttribute is null)
                {
                    return;
                }

                var isPartialRecord = namedTypeSymbol is { IsRecord: true, IsValueType: false }
                                      && namedTypeSymbol.DeclaringSyntaxReferences
                                          .Select(reference => reference.GetSyntax(symbolContext.CancellationToken))
                                          .OfType<RecordDeclarationSyntax>()
                                          .Any(recordDeclaration =>
                                              recordDeclaration.Modifiers.Any(modifier =>
                                                  modifier.IsKind(SyntaxKind.PartialKeyword)));

                if (isPartialRecord)
                {
                    return;
                }

                var location = unionAttribute.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken)
                                   .GetLocation()
                               ?? namedTypeSymbol.Locations.FirstOrDefault();

                if (location is not null)
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(_rule, location));
                }
            }, SymbolKind.NamedType);
        });
    }
}
