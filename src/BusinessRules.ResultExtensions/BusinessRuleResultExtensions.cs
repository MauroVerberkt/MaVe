using System.Diagnostics.Contracts;
using MaVe.Monads;

namespace MaVe.BusinessRules.ResultExtensions;

/// <summary>
/// Extension methods for integrating MaVe.BusinessRules with the Result monad pattern.
/// </summary>
public static class BusinessRuleResultExtensions
{
    /// <summary>
    /// Creates an <see cref="Error" /> from a <see cref="BusinessRuleViolationException" />.
    /// Uses the rule's key as the error code and preserves the exception for logging.
    /// </summary>
    /// <param name="exception">The business rule violation exception.</param>
    /// <returns>An <see cref="Error" /> with the violation message, rule key as code, and the original exception.</returns>
    [Pure]
    public static Error FromViolation(BusinessRuleViolationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Error(exception.Message, exception.Key, exception);
    }

    /// <summary>
    /// Converts a <see cref="BusinessRuleViolationException" /> to a failed <see cref="Result{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="exception">The business rule violation exception to convert.</param>
    /// <returns>A failed <see cref="Result{T}" /> containing the exception as an error.</returns>
    [Pure]
    public static Result<T> ToResult<T>(this BusinessRuleViolationException exception) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Result.Failure<T>(FromViolation(exception));
    }

    /// <summary>
    /// Creates a failed <see cref="Result{TData}" /> from a <see cref="BusinessRule{T}" /> with the specified error.
    /// </summary>
    /// <typeparam name="TRule">The type of the business rule.</typeparam>
    /// <typeparam name="TData">The type of the result data.</typeparam>
    /// <param name="rule">The business rule that was violated.</param>
    /// <param name="error">The exception that caused the violation.</param>
    /// <returns>A failed <see cref="Result{TData}" /> containing a <see cref="BusinessRuleViolationException" />.</returns>
    [Pure]
    public static Result<TData> ToResult<TRule, TData>(this BusinessRule<TRule> rule, Exception error)
        where TRule : BusinessRule<TRule>, new()
        where TData : notnull
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(error);

        var exception = new BusinessRuleViolationException(rule, error.Message, error);
        return Result.Failure<TData>(FromViolation(exception));
    }

    /// <summary>
    /// Executes an operation and wraps the result in a <see cref="Result{T}" />.
    /// If the operation throws a <see cref="BusinessRuleViolationException" />, it is captured in the result.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>
    /// A <see cref="Result{T}" /> containing either the successful result or the exception.
    /// </returns>
    [Pure]
    public static Result<T> ValidateAndReturn<T>(Func<T> operation) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var result = operation();
            return Result.Success(result);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<T>(FromViolation(ex));
        }
    }

    /// <summary>
    /// Executes an operation and wraps the result in a <see cref="Result{T}" />.
    /// If the operation throws a <see cref="BusinessRuleViolationException" />, it is captured in the result.
    /// This overload provides the business rule for context but does not enforce it.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="rule">The business rule context (for documentation purposes).</param>
    /// <returns>
    /// A <see cref="Result{T}" /> containing either the successful result or the exception.
    /// </returns>
    [Pure]
    public static Result<T> ValidateAndReturn<T>(Func<T> operation, BusinessRuleBase rule) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(rule);

        try
        {
            var result = operation();
            return Result.Success(result);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<T>(FromViolation(ex));
        }
    }

    /// <summary>
    /// Executes an asynchronous operation and wraps the result in a <see cref="Result{T}" />.
    /// If the operation throws a <see cref="BusinessRuleViolationException" />, it is captured in the result.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="Result{T}" />
    /// with either the successful result or the exception.
    /// </returns>
    [Pure]
    public static async Task<Result<T>> ValidateAndReturnAsync<T>(Func<Task<T>> operation) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var result = await operation();
            return Result.Success(result);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<T>(FromViolation(ex));
        }
    }

    /// <summary>
    /// Executes an asynchronous operation and wraps the result in a <see cref="Result{T}" />.
    /// If the operation throws a <see cref="BusinessRuleViolationException" />, it is captured in the result.
    /// This overload provides the business rule for context but does not enforce it.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <param name="rule">The business rule context (for documentation purposes).</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a <see cref="Result{T}" />
    /// with either the successful result or the exception.
    /// </returns>
    [Pure]
    public static async Task<Result<T>> ValidateAndReturnAsync<T>(Func<Task<T>> operation, BusinessRuleBase rule)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(rule);

        try
        {
            var result = await operation();
            return Result.Success(result);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure<T>(FromViolation(ex));
        }
    }

    /// <summary>
    /// Ensures that a value satisfies a business rule predicate.
    /// If the predicate fails, returns a failed <see cref="Result{T}" /> with a <see cref="BusinessRuleViolationException" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The predicate function to test the value against.</param>
    /// <param name="rule">The business rule that defines the validation.</param>
    /// <returns>
    /// A successful <see cref="Result{T}" /> containing the value if the predicate passes,
    /// otherwise a failed <see cref="Result{T}" /> with a <see cref="BusinessRuleViolationException" />.
    /// </returns>
    [Pure]
    public static Result<T> EnsureBusinessRule<T>(
        this T value,
        Func<T, bool> predicate,
        BusinessRuleBase rule) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(rule);

        return predicate(value)
            ? Result.Success(value)
            : Result.Failure<T>(FromViolation(new BusinessRuleViolationException(rule)));
    }

    /// <summary>
    /// Ensures that a value satisfies a business rule predicate with a custom error message.
    /// If the predicate fails, returns a failed <see cref="Result{T}" /> with a <see cref="BusinessRuleViolationException" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The predicate function to test the value against.</param>
    /// <param name="rule">The business rule that defines the validation.</param>
    /// <param name="errorMessage">Custom error message to use if the predicate fails.</param>
    /// <returns>
    /// A successful <see cref="Result{T}" /> containing the value if the predicate passes,
    /// otherwise a failed <see cref="Result{T}" /> with a <see cref="BusinessRuleViolationException" />.
    /// </returns>
    [Pure]
    public static Result<T> EnsureBusinessRule<T>(
        this T value,
        Func<T, bool> predicate,
        BusinessRuleBase rule,
        string errorMessage) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(errorMessage);

        return predicate(value)
            ? Result.Success(value)
            : Result.Failure<T>(FromViolation(new BusinessRuleViolationException(rule, errorMessage)));
    }

    /// <summary>
    /// Validates a value against multiple business rules.
    /// Returns a failed <see cref="Result{T}" /> on the first rule that fails.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="validations">
    /// An array of tuples containing predicates and their corresponding business rules.
    /// </param>
    /// <returns>
    /// A successful <see cref="Result{T}" /> containing the value if all predicates pass,
    /// otherwise a failed <see cref="Result{T}" /> with a <see cref="BusinessRuleViolationException" />
    /// for the first failing rule.
    /// </returns>
    [Pure]
    public static Result<T> ValidateAll<T>(
        this T value,
        params (Func<T, bool> predicate, BusinessRuleBase rule)[] validations) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(validations);

        foreach (var (predicate, rule) in validations)
        {
            if (predicate == null)
            {
                throw new ArgumentException("Validations array contains a null predicate.", nameof(validations));
            }

            if (rule == null)
            {
                throw new ArgumentException("Validations array contains a null rule.", nameof(validations));
            }

            if (!predicate(value))
            {
                return Result.Failure<T>(FromViolation(new BusinessRuleViolationException(rule)));
            }
        }

        return Result.Success(value);
    }

    /// <summary>
    /// Validates a value against multiple business rules with custom error messages.
    /// Returns a failed <see cref="Result{T}" /> on the first rule that fails.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="validations">
    /// An array of tuples containing predicates, their corresponding business rules, and custom error messages.
    /// </param>
    /// <returns>
    /// A successful <see cref="Result{T}" /> containing the value if all predicates pass,
    /// otherwise a failed <see cref="Result{T}" /> with a <see cref="BusinessRuleViolationException" />
    /// for the first failing rule.
    /// </returns>
    [Pure]
    public static Result<T> ValidateAll<T>(
        this T value,
        params (Func<T, bool> predicate, BusinessRuleBase rule, string errorMessage)[] validations)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(validations);

        foreach (var (predicate, rule, errorMessage) in validations)
        {
            if (predicate == null)
            {
                throw new ArgumentException("Validations array contains a null predicate.", nameof(validations));
            }

            if (rule == null)
            {
                throw new ArgumentException("Validations array contains a null rule.", nameof(validations));
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or whitespace in validations array",
                    nameof(validations));
            }

            if (!predicate(value))
            {
                return Result.Failure<T>(FromViolation(new BusinessRuleViolationException(rule, errorMessage)));
            }
        }

        return Result.Success(value);
    }

    /// <summary>
    /// Converts a failed <see cref="Result{T}" /> to a <see cref="BusinessRuleViolationException" />.
    /// Throws an <see cref="InvalidOperationException" /> if the result is already successful.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="rule">The business rule context for the exception.</param>
    /// <returns>A <see cref="BusinessRuleViolationException" /> wrapping the result's error.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is successful.</exception>
    [Pure]
    public static BusinessRuleViolationException ToBusinessRuleException<T>(
        this Result<T> result,
        BusinessRuleBase rule) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(rule);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert successful result to exception");
        }

        return result.Error.Exception switch
        {
            BusinessRuleViolationException exception => exception,
            null => throw new InvalidOperationException(
                "Cannot convert error to BusinessRuleViolationException: no inner exception present. " +
                "Use FromViolation() when creating errors from business rule violations."),
            _ => new BusinessRuleViolationException(rule, result.Error.Message, result.Error.Exception)
        };
    }
}
