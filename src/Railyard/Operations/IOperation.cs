using MaVe.Monads;

namespace MaVe.Railyard;

/// <summary>
/// Represents a Railyard operation.
/// </summary>
public interface IOperation
{
    /// <summary>
    /// Executes the operation using JSON input and returns JSON output.
    /// </summary>
    /// <param name="operationName">The operation dispatch name.</param>
    /// <param name="jsonInput">The serialized JSON input payload.</param>
    /// <param name="serializerOptions">Serialization options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing serialized output or an error.</returns>
    Task<Result<string>> PerformAsync(
        string operationName,
        string jsonInput,
        System.Text.Json.JsonSerializerOptions? serializerOptions,
        CancellationToken ct = default);
}
