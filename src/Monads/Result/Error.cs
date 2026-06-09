using System.Diagnostics.Contracts;

namespace MaVe.Monads;

/// <summary>
/// Represents an error in a failed <see cref="Result{TData}" />.
/// Extend with your own static factory methods for domain-specific errors.
/// </summary>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="Code">An optional machine-readable error code for programmatic handling.</param>
/// <param name="Exception">The original exception, if this error was created from a caught exception.</param>
public record Error(string Message, string? Code = null, Exception? Exception = null)
{
    /// <summary>
    /// Creates an error with the specified message and optional code.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="code">An optional machine-readable error code.</param>
    [Pure]
    public static Error Create(string message, string? code = null)
    {
        return new Error(message, code);
    }

    /// <summary>
    /// Creates an error from an unexpected exception.
    /// Use this at boundary code where exceptions are caught and converted to Results.
    /// </summary>
    /// <param name="exception">The exception that was caught.</param>
    [Pure]
    public static Error Unexpected(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Error(exception.Message, "UNEXPECTED", exception);
    }
}
