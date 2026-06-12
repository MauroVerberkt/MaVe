using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MaVe.RailyardGenerator;

/// <summary>
/// Generates Railyard DI registration and dispatch implementation.
/// </summary>
[Generator]
public sealed class RailyardSourceGenerator : IIncrementalGenerator
{
    private const string OperationAttributeFullyQualifiedName = "MaVe.Railyard.OperationAttribute";
    private const string OperationBaseFullyQualifiedName = "MaVe.Railyard.Operation<TInput, TOutput>";
    private const string SyncOperationBaseFullyQualifiedName = "MaVe.Railyard.SyncOperation<TInput, TOutput>";

    private static readonly DiagnosticDescriptor _duplicateOperationNameDescriptor = new(
        "RY1001",
        "Duplicate operation name",
        "Operation name '{0}' is declared more than once",
        "Railyard",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor _invalidOperationBaseDescriptor = new(
        "RY1002",
        "Invalid operation base type",
        "Operation '{0}' must inherit from Operation<TInput, TOutput> or SyncOperation<TInput, TOutput>",
        "Railyard",
        DiagnosticSeverity.Error,
        true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var operationCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => GetOperationCandidate(syntaxContext))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!);

        context.RegisterSourceOutput(operationCandidates.Collect(), Emit);
    }

    private static OperationCandidate? GetOperationCandidate(GeneratorSyntaxContext syntaxContext)
    {
        if (syntaxContext.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        if (syntaxContext.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var operationAttribute = classSymbol
            .GetAttributes()
            .FirstOrDefault(attribute =>
                attribute.AttributeClass?.ToDisplayString() == OperationAttributeFullyQualifiedName);

        if (operationAttribute is null)
        {
            return null;
        }

        if (operationAttribute.ConstructorArguments.Length == 0 ||
            operationAttribute.ConstructorArguments[0].Value is not string operationName)
        {
            return null;
        }

        string? description = null;
        foreach (var namedArgument in operationAttribute.NamedArguments)
        {
            if (namedArgument is { Key: "Description", Value.Value: string value })
            {
                description = value;
                break;
            }
        }

        return new OperationCandidate(
            operationName,
            description,
            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.Locations.FirstOrDefault(),
            HasValidOperationBase(classSymbol));
    }

    private static bool HasValidOperationBase(INamedTypeSymbol classSymbol)
    {
        var current = classSymbol;
        while (current.BaseType is not null)
        {
            current = current.BaseType;
            var baseName = current.OriginalDefinition.ToDisplayString();

            if (baseName is OperationBaseFullyQualifiedName or SyncOperationBaseFullyQualifiedName)
            {
                return true;
            }
        }

        return false;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<OperationCandidate> candidates)
    {
        if (candidates.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var invalidCandidate in candidates.Where(candidate => !candidate.HasValidBase))
        {
            if (invalidCandidate.Location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _invalidOperationBaseDescriptor,
                    invalidCandidate.Location,
                    invalidCandidate.ClassName));
            }
        }

        var validCandidates = candidates.Where(candidate => candidate.HasValidBase).ToList();
        var duplicateGroups = validCandidates
            .GroupBy(candidate => candidate.OperationName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToList();

        foreach (var duplicateGroup in duplicateGroups)
        {
            foreach (var duplicate in duplicateGroup)
            {
                if (duplicate.Location is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        _duplicateOperationNameDescriptor,
                        duplicate.Location,
                        duplicate.OperationName));
                }
            }
        }

        var duplicateNames = new HashSet<string>(duplicateGroups.Select(group => group.Key), StringComparer.Ordinal);
        var generationCandidates = validCandidates
            .Where(candidate => !duplicateNames.Contains(candidate.OperationName))
            .OrderBy(candidate => candidate.OperationName, StringComparer.Ordinal)
            .ToList();

        var source = GenerateSource(generationCandidates);
        context.AddSource("Railyard.Generated.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateSource(IReadOnlyList<OperationCandidate> candidates)
    {
        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("namespace MaVe.Railyard;");
        builder.AppendLine();
        builder.AppendLine("public static class RailyardServiceCollectionExtensions");
        builder.AppendLine("{");
        builder.AppendLine(
            "    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddRailyard(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(services);");
        builder.AppendLine();

        foreach (var candidate in candidates)
        {
            builder.AppendLine(
                $"        services.Add(global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Transient(typeof({candidate.FullyQualifiedTypeName}), typeof({candidate.FullyQualifiedTypeName})));");
        }

        builder.AppendLine(
            "        services.Add(global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton(typeof(IYard), serviceProvider => new GeneratedYard(serviceProvider)));");
        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("internal sealed class GeneratedYard : IYard");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly global::System.IServiceProvider _serviceProvider;");
        builder.AppendLine(
            "    private readonly global::System.Collections.Generic.Dictionary<string, global::System.Func<global::System.IServiceProvider, IOperation>> _factoryByName;");
        builder.AppendLine(
            "    private readonly global::System.Collections.Generic.Dictionary<string, OperationDescriptor> _descriptorByName;");
        builder.AppendLine();
        builder.AppendLine("    public GeneratedYard(global::System.IServiceProvider serviceProvider)");
        builder.AppendLine("    {");
        builder.AppendLine("        _serviceProvider = serviceProvider;");
        builder.AppendLine(
            "        _factoryByName = new global::System.Collections.Generic.Dictionary<string, global::System.Func<global::System.IServiceProvider, IOperation>>(global::System.StringComparer.Ordinal)");
        builder.AppendLine("        {");

        foreach (var candidate in candidates)
        {
            builder.AppendLine(
                $"            [\"{Escape(candidate.OperationName)}\"] = sp => (IOperation)(sp.GetService(typeof({candidate.FullyQualifiedTypeName})) ?? throw new global::System.InvalidOperationException(\"Service '{candidate.FullyQualifiedTypeName}' is not registered.\")),");
        }

        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine(
            "        _descriptorByName = new global::System.Collections.Generic.Dictionary<string, OperationDescriptor>(global::System.StringComparer.Ordinal)");
        builder.AppendLine("        {");

        foreach (var candidate in candidates)
        {
            var descriptionLiteral = candidate.Description is null
                ? "null"
                : $"\"{Escape(candidate.Description)}\"";

            builder.AppendLine(
                $"            [\"{Escape(candidate.OperationName)}\"] = new OperationDescriptor(\"{Escape(candidate.OperationName)}\", {descriptionLiteral}),");
        }

        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine(
            "        Manifest = global::System.Array.AsReadOnly(global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.OrderBy(_descriptorByName.Values, descriptor => descriptor.Name, global::System.StringComparer.Ordinal)));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine(
            "    public global::System.Collections.Generic.IReadOnlyList<OperationDescriptor> Manifest { get; }");
        builder.AppendLine();
        builder.AppendLine("    public OperationDescriptor? TryGetDescriptor(string operationName)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(operationName);");
        builder.AppendLine(
            "        return _descriptorByName.TryGetValue(operationName, out var descriptor) ? descriptor : null;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine(
            "    public async global::System.Threading.Tasks.Task<global::MaVe.Monads.Result<string>> DispatchAsync(string operationName, string jsonInput, global::System.Threading.CancellationToken ct = default)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(operationName);");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(jsonInput);");
        builder.AppendLine();
        builder.AppendLine("        if (!_factoryByName.TryGetValue(operationName, out var factory))");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            return global::MaVe.Monads.Result.Failure<string>(RailyardErrors.OperationNotFound(operationName));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var operation = factory(_serviceProvider);");
        builder.AppendLine(
            "        var serializerOptions = _serviceProvider.GetService(typeof(global::System.Text.Json.JsonSerializerOptions)) as global::System.Text.Json.JsonSerializerOptions;");
        builder.AppendLine(
            "        var result = await operation.PerformAsync(jsonInput, serializerOptions, ct).ConfigureAwait(false);");
        builder.AppendLine("        if (result.IsSuccess)");
        builder.AppendLine("        {");
        builder.AppendLine("            return result;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        var contextualizedError = result.Error! with { Message = $\"Operation '{operationName}': {result.Error.Message}\" };");
        builder.AppendLine("        return global::MaVe.Monads.Result.Failure<string>(contextualizedError);");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private sealed class OperationCandidate(
        string operationName,
        string? description,
        string fullyQualifiedTypeName,
        string className,
        Location? location,
        bool hasValidBase)
    {
        public string OperationName { get; } = operationName;

        public string? Description { get; } = description;

        public string FullyQualifiedTypeName { get; } = fullyQualifiedTypeName;

        public string ClassName { get; } = className;

        public Location? Location { get; } = location;

        public bool HasValidBase { get; } = hasValidBase;
    }
}
