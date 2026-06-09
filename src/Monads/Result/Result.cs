using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace MaVe.Monads;

/// <summary>
/// Represents the result of an operation, indicating success or failure, along with additional information.
/// </summary>
public sealed class Result<TData> : IEquatable<Result<TData>> where TData : notnull
{
    private const string NoDataProvidedMessage = "Data must be provided for a successful result.";
    private const string NoErrorProvidedMessage = "Error must be provided for a failed result.";

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TData}" /> class with the specified parameters.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="data">Data associated with the <see cref="Result{TData}" />.</param>
    /// <param name="error">The error associated with the failure, if any.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="isSuccess" /> is true and <paramref name="data" /> is null.
    /// </exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    private Result(bool isSuccess, TData? data, Error? error)
    {
        IsSuccess = isSuccess;

        if (isSuccess)
        {
            ArgumentNullException.ThrowIfNull(data, NoDataProvidedMessage);
            Data = data;
            Error = null; // Ensure Error is null for success
        }
        else
        {
            ArgumentNullException.ThrowIfNull(error, NoErrorProvidedMessage);
            Error = error;
            Data = default; // Ensure Data is default for failure
        }
    }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    [Pure]
    [MemberNotNullWhen(true, nameof(Data))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    [Pure]
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Data))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Data associated with the <see cref="Result{TData}" />.
    /// </summary>
    [Pure]
    public TData? Data { get; }

    /// <summary>
    /// The error associated with a failed operation.
    /// </summary>
    [Pure]
    public Error? Error { get; }

    /// <inheritdoc />
    [Pure]
    public bool Equals(Result<TData>? other)
    {
        return other != null &&
               IsSuccess == other.IsSuccess &&
               EqualityComparer<TData>.Default.Equals(Data, other.Data) &&
               EqualityComparer<Error>.Default.Equals(Error, other.Error);
    }

    /// <summary>
    /// Chains with another operation if successful; otherwise returns the current failure.
    /// </summary>
    /// <param name="function">The function to invoke on success.</param>
    [Pure]
    public Result<TData> Then(Func<Result<TData>> function)
    {
        return IsSuccess ? function() : this;
    }

    /// <inheritdoc cref="Then" />
    /// <typeparam name="TNewData">The type of the new data to transform to.</typeparam>
    [Pure]
    public Result<TNewData> Then<TNewData>(Func<Result<TNewData>> function)
        where TNewData : notnull
    {
        return IsSuccess ? function() : Result<TNewData>.Failure(Error);
    }

    /// <summary>
    /// Chains with an operation that transforms to a different result type, passing the current data.
    /// </summary>
    /// <typeparam name="TNewData">The type of the new data to transform to.</typeparam>
    /// <param name="function">The function that receives the current data and produces a new result.</param>
    [Pure]
    public Result<TNewData> Bind<TNewData>(Func<TData, Result<TNewData>> function)
        where TNewData : notnull
    {
        return IsSuccess ? function(Data) : Result<TNewData>.Failure(Error);
    }

    /// <inheritdoc cref="Bind" />
    public async Task<Result<TNewData>> BindAsync<TNewData>(Func<TData, Task<Result<TNewData>>> function)
        where TNewData : notnull
    {
        return IsSuccess ? await function(Data).ConfigureAwait(false) : Result<TNewData>.Failure(Error);
    }

    /// <inheritdoc cref="Bind" />
    public async Task<Result<TNewData>> BindAsync<TNewData>(
        Func<TData, CancellationToken, Task<Result<TNewData>>> function, CancellationToken cancellationToken)
        where TNewData : notnull
    {
        return IsSuccess
            ? await function(Data, cancellationToken).ConfigureAwait(false)
            : Result<TNewData>.Failure(Error);
    }

    /// <summary>
    /// Transforms the data if successful; otherwise propagates the failure.
    /// </summary>
    /// <typeparam name="TNewData">The type of the new data to transform to.</typeparam>
    /// <param name="transform">The function that transforms the current data.</param>
    [Pure]
    public Result<TNewData> Map<TNewData>(Func<TData, TNewData> transform) where TNewData : notnull
    {
        if (IsFailure)
        {
            return Result<TNewData>.Failure(Error);
        }

        var newData = transform(Data);
        return Result<TNewData>.Success(newData);
    }

    /// <inheritdoc cref="Map" />
    public async Task<Result<TNewData>> MapAsync<TNewData>(Func<TData, Task<TNewData>> transform)
        where TNewData : notnull
    {
        if (IsFailure)
        {
            return Result<TNewData>.Failure(Error);
        }

        var newData = await transform(Data).ConfigureAwait(false);
        return Result<TNewData>.Success(newData);
    }

    /// <inheritdoc cref="Map" />
    public async Task<Result<TNewData>> MapAsync<TNewData>(
        Func<TData, CancellationToken, Task<TNewData>> transform, CancellationToken cancellationToken)
        where TNewData : notnull
    {
        if (IsFailure)
        {
            return Result<TNewData>.Failure(Error);
        }

        var newData = await transform(Data, cancellationToken).ConfigureAwait(false);
        return Result<TNewData>.Success(newData);
    }

    /// <summary>
    /// Projects the data into a new form. Enables LINQ <c>select</c> syntax.
    /// </summary>
    /// <param name="selector">The projection function.</param>
    [Pure]
    public Result<TNewData> Select<TNewData>(Func<TData, TNewData> selector) where TNewData : notnull
    {
        return Map(selector);
    }

    /// <summary>
    /// Projects the data into an intermediate result and applies a result selector.
    /// Enables LINQ <c>from x in ... from y in ... select ...</c> syntax.
    /// </summary>
    /// <param name="selector">The function that projects to an intermediate result.</param>
    /// <param name="resultSelector">The function that combines original and intermediate data.</param>
    [Pure]
    public Result<TResult> SelectMany<TNewData, TResult>(
        Func<TData, Result<TNewData>> selector,
        Func<TData, TNewData, TResult> resultSelector)
        where TNewData : notnull
        where TResult : notnull
    {
        if (IsFailure)
        {
            return Result<TResult>.Failure(Error);
        }

        var intermediate = selector(Data);

        if (intermediate.IsFailure)
        {
            return Result<TResult>.Failure(intermediate.Error);
        }

        var result = resultSelector(Data, intermediate.Data);
        return Result<TResult>.Success(result);
    }

    /// <summary>
    /// Executes an action only if the <see cref="Result{TData}" /> is successful.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This result.</returns>
    public Result<TData> OnSuccess(Action<TData> action)
    {
        if (IsSuccess)
        {
            action(Data);
        }

        return this;
    }

    /// <summary>
    /// Executes an action only if the <see cref="Result{TData}" /> has failed.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This result.</returns>
    public Result<TData> OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            action(Error);
        }

        return this;
    }

    /// <summary>
    /// Executes an action regardless of whether the <see cref="Result{TData}" /> is successful or failed.
    /// Useful for logging, tracing, or other side effects that should occur in both cases.
    /// </summary>
    /// <param name="action">The action to execute, receiving this result.</param>
    /// <returns>This result.</returns>
    public Result<TData> Tap(Action<Result<TData>> action)
    {
        action(this);
        return this;
    }

    /// <summary>
    /// Deconstructs the result into its components for pattern matching.
    /// </summary>
    [Pure]
    public void Deconstruct(out bool isSuccess, out TData? data, out Error? error)
    {
        isSuccess = IsSuccess;
        data = Data;
        error = Error;
    }

    /// <summary>
    /// Applies a function based on whether this result is a success or failure.
    /// </summary>
    /// <param name="onSuccess">The function to invoke for success.</param>
    /// <param name="onFailure">The function to invoke for failure.</param>
    [Pure]
    public TResult Match<TResult>(Func<TData, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Data) : onFailure(Error);
    }

    /// <inheritdoc cref="Match" />
    public async Task<TResult> MatchAsync<TResult>(
        Func<TData, Task<TResult>> onSuccess,
        Func<Error, Task<TResult>> onFailure)
    {
        return IsSuccess
            ? await onSuccess(Data).ConfigureAwait(false)
            : await onFailure(Error).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Match" />
    public async Task<TResult> MatchAsync<TResult>(
        Func<TData, CancellationToken, Task<TResult>> onSuccess,
        Func<Error, CancellationToken, Task<TResult>> onFailure,
        CancellationToken cancellationToken)
    {
        return IsSuccess
            ? await onSuccess(Data, cancellationToken).ConfigureAwait(false)
            : await onFailure(Error, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Then" />
    public async Task<Result<TData>> ThenAsync(Func<Task<Result<TData>>> function)
    {
        return IsSuccess ? await function().ConfigureAwait(false) : this;
    }

    /// <inheritdoc cref="Then" />
    public async Task<Result<TData>> ThenAsync(
        Func<CancellationToken, Task<Result<TData>>> function, CancellationToken cancellationToken)
    {
        return IsSuccess ? await function(cancellationToken).ConfigureAwait(false) : this;
    }

    /// <inheritdoc cref="Then" />
    /// <typeparam name="TNewData">The type of the new data to transform to.</typeparam>
    public async Task<Result<TNewData>> ThenAsync<TNewData>(Func<Task<Result<TNewData>>> function)
        where TNewData : notnull
    {
        return IsSuccess ? await function().ConfigureAwait(false) : Result<TNewData>.Failure(Error);
    }

    /// <inheritdoc cref="Then" />
    /// <typeparam name="TNewData">The type of the new data to transform to.</typeparam>
    public async Task<Result<TNewData>> ThenAsync<TNewData>(
        Func<CancellationToken, Task<Result<TNewData>>> function, CancellationToken cancellationToken)
        where TNewData : notnull
    {
        return IsSuccess ? await function(cancellationToken).ConfigureAwait(false) : Result<TNewData>.Failure(Error);
    }

    /// <inheritdoc />
    [Pure]
    public override bool Equals(object? obj)
    {
        return obj is Result<TData> other && Equals(other);
    }

    /// <summary>
    /// Determines whether two <see cref="Result{TData}" /> instances are equal.
    /// </summary>
    [Pure]
    public static bool operator ==(Result<TData>? left, Result<TData>? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    /// <summary>
    /// Determines whether two <see cref="Result{TData}" /> instances are not equal.
    /// </summary>
    [Pure]
    public static bool operator !=(Result<TData>? left, Result<TData>? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    [Pure]
    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Data, Error);
    }

    /// <inheritdoc />
    [Pure]
    public override string ToString()
    {
        return IsSuccess ? $"Success: {Data}" : $"Failure: {Error.Message}";
    }

    /// <summary>
    /// Creates a successful result instance.
    /// </summary>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static Result<TData> Success(TData data)
    {
        return new Result<TData>(true, data, null);
    }

    /// <summary>
    /// Creates a failed result instance.
    /// </summary>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static Result<TData> Failure(Error error)
    {
        return new Result<TData>(false, default, error);
    }
}

/// <summary>
/// Non-generic factory class for <see cref="Result{TData}" />>
/// </summary>
public static class Result
{
    /// <inheritdoc cref="Result{TData}.Success" />
    [Pure]
    public static Result<TData> Success<TData>(TData data) where TData : notnull
    {
        return Result<TData>.Success(data);
    }

    /// <inheritdoc cref="Result{TData}.Failure" />
    [Pure]
    public static Result<TData> Failure<TData>(Error error) where TData : notnull
    {
        return Result<TData>.Failure(error);
    }
}
