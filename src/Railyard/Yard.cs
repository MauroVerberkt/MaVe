using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MaVe.Monads;
using MaVe.Railyard.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace MaVe.Railyard;

/// <summary>
/// Provides a collection of operations and allows retrieving them by name.
/// This class uses reflection to discover and register operation types dynamically.
/// </summary>
[RequiresUnreferencedCode("This code uses reflection to get all implemented operations.")]
public class Yard : IYard
{
    /// <summary>
    /// The name of the "Name" static field in an Operation class.
    /// </summary>
    private const string NameFieldName = "Name";

    /// <summary>
    /// The name of the "Info" static field in an Operation class.
    /// </summary>
    private const string InfoFieldName = "Info";

    /// <summary>
    /// A read-only dictionary mapping operation names to their corresponding operation types.
    /// </summary>
    private readonly ReadOnlyDictionary<string, Type> _operationMappings;

    /// <summary>
    /// An array of required service types that must be available in the service collection.
    /// </summary>
    private readonly Type[] _requiredServices =
    [
    ];

    /// <summary>
    /// The service provider used to resolve operation instances.
    /// </summary>
    private readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Yard" /> class.
    /// </summary>
    /// <param name="serviceCollection">The service collection used to register operations and services.</param>
    /// <exception cref="MissingServiceException">Thrown when any required services are missing from the collection.</exception>
    public Yard(IServiceCollection serviceCollection)
    {
        ValidateServices(serviceCollection);

        var operationTypes = GetOperationTypes();
        foreach (var operationType in operationTypes)
        {
            serviceCollection.AddTransient(operationType);
        }

        var operationMappings = OperationMappingDictionary(operationTypes);
        _operationMappings = new ReadOnlyDictionary<string, Type>(operationMappings);

        OperationsInfo = OperationInfoDictionary(operationTypes);

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    /// <inheritdoc />
    public InfoDictionary OperationsInfo { get; }

    /// <inheritdoc />
    public Option<IOperation> GetOperationByName(string operationName)
    {
        if (!_operationMappings.TryGetValue(operationName, out var operationType))
        {
            return Option<IOperation>.None;
        }

        var operation = _serviceProvider.GetService(operationType) as IOperation;
        return Option<IOperation>.FromNullable(operation);
    }

    /// <summary>
    /// Validates that all required services are registered in the provided service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection to validate.</param>
    /// <exception cref="MissingServiceException">Thrown if any required services are missing.</exception>
    private void ValidateServices(IServiceCollection serviceCollection)
    {
        var missingServices =
            _requiredServices
                .Where(serviceType =>
                    serviceCollection.All(descriptor => descriptor.ServiceType != serviceType))
                .ToList();

        if (!missingServices.Any())
        {
            return;
        }

        throw new MissingServiceException(missingServices.Select(type => type.Name));
    }

    /// <summary>
    /// Gets all operation types by scanning the current domain's assemblies.
    /// </summary>
    /// <returns>A list of operation types implementing the <see cref="IOperation" /> interface.</returns>
    private static List<Type> GetOperationTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                typeof(IOperation).IsAssignableFrom(type) &&
                type is
                {
                    IsInterface: false,
                    IsAbstract: false
                })
            .ToList();
    }

    /// <summary>
    /// Creates a dictionary mapping operation names to their corresponding operation types.
    /// </summary>
    /// <param name="operationTypes">The list of operation types to map.</param>
    /// <returns>A dictionary mapping operation names to operation types.</returns>
    private static ConcurrentDictionary<string, Type> OperationMappingDictionary(List<Type> operationTypes)
    {
        var operationMappings = new ConcurrentDictionary<string, Type>();

        foreach (var operationType in operationTypes)
        {
            var nameString = GetPrivateConstantStringFieldValue(operationType, NameFieldName);
            operationMappings[nameString] = operationType;
        }

        return operationMappings;
    }

    /// <summary>
    /// Retrieves the value of a private constant string field from an operation type.
    /// </summary>
    /// <param name="operationType">The operation type to inspect.</param>
    /// <param name="fieldName">The name of the constant field to retrieve.</param>
    /// <returns>The value of the constant field.</returns>
    /// <exception cref="MissingFieldException">Thrown if the field does not exist or is not a valid string.</exception>
    private static string GetPrivateConstantStringFieldValue(Type operationType, string fieldName)
    {
        var assertInvalidMessage =
            $"{operationType.FullName} should have a \"private const {fieldName}\" field. With a valid string value.";

        var field = operationType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);

        if (field == null)
        {
            throw new MissingFieldException(assertInvalidMessage);
        }

        var fieldValue = field.GetValue(null);

        if (fieldValue is not string fieldValueString)
        {
            throw new MissingFieldException(assertInvalidMessage);
        }

        return fieldValueString;
    }

    /// <summary>
    /// Creates a dictionary of operation names and corresponding operation info.
    /// </summary>
    /// <param name="operationTypes">The list of operation types to map to info.</param>
    /// <returns>A dictionary mapping operation names to operation info.</returns>
    private static InfoDictionary OperationInfoDictionary(List<Type> operationTypes)
    {
        var operationsInfo = new ConcurrentDictionary<string, string>();

        foreach (var operationType in operationTypes)
        {
            var nameString = GetPrivateConstantStringFieldValue(operationType, NameFieldName);
            var infoString = GetPrivateConstantStringFieldValue(operationType, InfoFieldName);
            operationsInfo[nameString] = infoString;
        }

        return new InfoDictionary(operationsInfo);
    }
}
