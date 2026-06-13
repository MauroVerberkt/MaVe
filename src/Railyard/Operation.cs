using System.Text.Json;
using MaVe.Monads;

namespace MaVe.Railyard;

/// <summary>
/// Base class for asynchronous Railyard operations.
/// </summary>
/// <typeparam name="TInput">Input type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public abstract class Operation<TInput, TOutput> : IOperation where TInput : class where TOutput : class
{
    /// <inheritdoc />
    public async Task<Result<string>> PerformAsync(
        string jsonInput,
        JsonSerializerOptions? serializerOptions,
        CancellationToken ct = default)
    {
        TInput? deserializedInput;
        try
        {
            deserializedInput = JsonSerializer.Deserialize<TInput>(jsonInput, serializerOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Failure<string>(RailyardErrors.InvalidInput(exception.Message));
        }

        if (deserializedInput == null)
        {
            return Result.Failure<string>(RailyardErrors.InputMustNotBeNull());
        }

        var validationResult = Validate(deserializedInput);
        if (validationResult.IsFailure)
        {
            return Result.Failure<string>(validationResult.Error!);
        }

        var executionResult = await ExecuteAsync(validationResult.Data!, ct).ConfigureAwait(false);
        if (executionResult.IsFailure)
        {
            return Result.Failure<string>(executionResult.Error!);
        }

        try
        {
            var serializedOutput = JsonSerializer.Serialize(executionResult.Data, serializerOptions);
            return Result.Success(serializedOutput);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Failure<string>(RailyardErrors.SerializationFailed(exception.Message));
        }
    }

    /// <summary>
    /// Validates the deserialized input.
    /// </summary>
    /// <param name="input">The deserialized input.</param>
    /// <returns>Success with input or failure with validation error.</returns>
    protected virtual Result<TInput> Validate(TInput input)
    {
        return Result.Success(input);
    }

    /// <summary>
    /// Executes operation logic.
    /// </summary>
    /// <param name="input">Validated input.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success with output or failure with execution error.</returns>
    protected abstract Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct);
}

/// <summary>
/// Convenience base class for synchronous operations.
/// </summary>
/// <typeparam name="TInput">Input type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public abstract class SyncOperation<TInput, TOutput> : Operation<TInput, TOutput>
    where TInput : class where TOutput : class
{
    /// <inheritdoc />
    protected sealed override Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct)
    {
        return Task.FromResult(Execute(input));
    }

    /// <summary>
    /// Executes operation logic synchronously.
    /// </summary>
    /// <param name="input">Validated input.</param>
    /// <returns>Success with output or failure with execution error.</returns>
    protected abstract Result<TOutput> Execute(TInput input);
}
