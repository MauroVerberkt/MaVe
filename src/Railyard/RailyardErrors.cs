using MaVe.Monads;

namespace MaVe.Railyard;

/// <summary>
/// Contains domain-specific errors used by the Railyard library.
/// </summary>
public static class RailyardErrors
{
    private const string InvalidInputCode = "RY001";
    private const string OperationNotFoundCode = "RY002";
    private const string SerializationFailedCode = "RY003";

    /// <summary>
    /// Creates an error indicating that the provided input could not be deserialized.
    /// </summary>
    /// <param name="detail">Optional error detail.</param>
    /// <returns>
    /// An <see cref="Error" /> with code <c>RY001</c>.
    /// </returns>
    public static Error InvalidInput(string? detail = null)
    {
        var detailSuffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        return Error.Create($"Input could not be deserialized.{detailSuffix}", InvalidInputCode);
    }

    /// <summary>
    /// Creates an error indicating that deserialized input was null.
    /// </summary>
    /// <returns>An <see cref="Error" /> with code <c>RY001</c>.</returns>
    public static Error InputMustNotBeNull()
    {
        return Error.Create("Input must not be null.", InvalidInputCode);
    }

    /// <summary>
    /// Creates an error indicating that no operation was found with the supplied name.
    /// </summary>
    /// <param name="operationName">Operation dispatch name.</param>
    /// <returns>An <see cref="Error" /> with code <c>RY002</c>.</returns>
    public static Error OperationNotFound(string operationName)
    {
        return Error.Create($"No operation registered with name '{operationName}'.", OperationNotFoundCode);
    }

    /// <summary>
    /// Creates an error indicating that operation output could not be serialized.
    /// </summary>
    /// <param name="detail">Optional error detail.</param>
    /// <returns>An <see cref="Error" /> with code <c>RY003</c>.</returns>
    public static Error SerializationFailed(string? detail = null)
    {
        var detailSuffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        return Error.Create($"Output serialization failed.{detailSuffix}", SerializationFailedCode);
    }
}
