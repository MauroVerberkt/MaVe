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

    /// <summary>
    /// Chains with another operation that may produce a different result type if successful; otherwise propagates failure.
    /// </summary>
    /// <typeparam name="TNewData">The type of the new data to transform to.</typeparam>
    /// <param name="function">The function to invoke on success.</param>
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

    /// <inheritdoc cref="Result{TData}.Bind{TNewData}(System.Func{TData, Result{TNewData}})" />
    [Pure]
    public async Task<Result<TNewData>> BindAsync<TNewData>(Func<TData, Task<Result<TNewData>>> function)
        where TNewData : notnull
    {
        return IsSuccess ? await function(Data).ConfigureAwait(false) : Result<TNewData>.Failure(Error);
    }

    /// <inheritdoc cref="Result{TData}.Bind{TNewData}(System.Func{TData, Result{TNewData}})" />
    [Pure]
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

    /// <inheritdoc cref="Result{TData}.Map{TNewData}(Func{TData, TNewData})" />
    [Pure]
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

    /// <inheritdoc cref="Result{TData}.Map{TNewData}(Func{TData, TNewData})" />
    [Pure]
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

    /// <inheritdoc cref="Then(Func{Result{TData}})" />
    [Pure]
    public async Task<Result<TData>> ThenAsync(Func<Task<Result<TData>>> function)
    {
        return IsSuccess ? await function().ConfigureAwait(false) : this;
    }

    /// <inheritdoc cref="Then(Func{Result{TData}})" />
    [Pure]
    public async Task<Result<TData>> ThenAsync(
        Func<CancellationToken, Task<Result<TData>>> function, CancellationToken cancellationToken)
    {
        return IsSuccess ? await function(cancellationToken).ConfigureAwait(false) : this;
    }

    /// <inheritdoc cref="Then{TNewData}(Func{Result{TNewData}})" />
    [Pure]
    public async Task<Result<TNewData>> ThenAsync<TNewData>(Func<Task<Result<TNewData>>> function)
        where TNewData : notnull
    {
        return IsSuccess ? await function().ConfigureAwait(false) : Result<TNewData>.Failure(Error);
    }

    /// <inheritdoc cref="Then{TNewData}(Func{Result{TNewData}})" />
    [Pure]
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
        if (obj is Result<TData> other)
        {
            return Equals(other);
        }

        return false;
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
