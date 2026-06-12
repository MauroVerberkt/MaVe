using MaVe.Monads;

namespace MaVe.Railyard;

/// <summary>
/// Dispatch entry point for Railyard operations.
/// </summary>
public interface IYard
{
    /// <summary>
    /// Gets all registered operation descriptors.
    /// </summary>
    public IReadOnlyList<OperationDescriptor> Manifest { get; }

    /// <summary>
    /// Dispatches a named operation with a JSON payload.
    /// </summary>
    /// <param name="operationName">Operation dispatch name.</param>
    /// <param name="jsonInput">JSON input payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Each dispatch resolves operations from a dedicated dependency injection scope created by the generated yard.
    /// Operation dependencies do not share the caller's ambient scope.
    /// </remarks>
    /// <returns>
    /// Success with JSON-serialized output, or failure with dispatch/validation/execution error.
    /// </returns>
    public Task<Result<string>> DispatchAsync(string operationName, string jsonInput, CancellationToken ct = default);

    /// <summary>
    /// Looks up an operation descriptor by name.
    /// </summary>
    /// <param name="operationName">Operation dispatch name.</param>
    /// <returns>The descriptor if found; otherwise <see langword="null" />.</returns>
    public OperationDescriptor? TryGetDescriptor(string operationName);
}
