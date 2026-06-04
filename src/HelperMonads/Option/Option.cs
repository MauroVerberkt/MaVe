using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace HelperMonads;

/// <summary>
/// Represents an abstract base class for an option that contains a value or is empty.
/// </summary>
/// <typeparam name="TValue">The type of the value, which must be a reference type (class).</typeparam>
public abstract class Option<TValue> : IEquatable<Option<TValue>> where TValue : notnull
{
    /// <summary>
    /// Gets an instance representing no value.
    /// </summary>
    [Pure]
    public static Option<TValue> None { get; } = new None<TValue>();

    /// <summary>
    /// Gets a value indicating whether the option contains a value.
    /// </summary>
    [Pure]
    public abstract bool HasValue { get; }

    /// <summary>
    /// Gets the value contained within the option.
    /// </summary>
    public abstract TValue Value { get; }

    /// <inheritdoc />
    [Pure]
    public bool Equals(Option<TValue>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (HasValue != other.HasValue)
        {
            return false;
        }

        return !HasValue || EqualityComparer<TValue>.Default.Equals(Value, other.Value);
    }

    /// <summary>
    /// Applies a function to the value if present, otherwise applies a function for when no value is present.
    /// </summary>
    /// <typeparam name="TResult">The result type of the match function.</typeparam>
    /// <param name="some">The function to apply if the option contains a value.</param>
    /// <param name="none">The function to apply if the option does not contain a value.</param>
    /// <returns>The result of the appropriate function based on the option's value presence.</returns>
    /// <exception cref="OptionNotPresentException">
    /// Thrown if the option is in an invalid state and neither a <see cref="Some{TValue}" /> nor <see cref="None{TValue}" />.
    /// </exception>
    public TResult Match<TResult>(Func<TValue, TResult> some, Func<TResult> none)
    {
        return this switch
        {
            Some<TValue> someOption => some(someOption.Value),
            None<TValue> => none(),
            _ => throw new OptionNotPresentException()
        };
    }

    /// <summary>
    /// Applies an asynchronous function to the value if present, otherwise applies an asynchronous function for when no value
    /// is
    /// present.
    /// </summary>
    /// <param name="some">The asynchronous function to apply if the option contains a value.</param>
    /// <param name="none">The asynchronous function to apply if the option does not contain a value.</param>
    /// <typeparam name="TResult">The result type of the match function.</typeparam>
    /// <returns>A task representing the result of the appropriate function based on the option's value presence.</returns>
    /// <exception cref="OptionNotPresentException">
    /// Thrown if the option is in an invalid state and neither a <see cref="Some{TValue}" /> nor <see cref="None{TValue}" />.
    /// </exception>
    public async Task<TResult> MatchAsync<TResult>(Func<TValue, Task<TResult>> some, Func<Task<TResult>> none)
    {
        return this switch
        {
            Some<TValue> someOption => await some(someOption.Value),
            None<TValue> => await none(),
            _ => throw new OptionNotPresentException()
        };
    }

    /// <inheritdoc cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, Task{TResult}}, Func{Task{TResult}})" />
    public async Task<TResult> MatchAsync<TResult>(
        Func<TValue, CancellationToken, Task<TResult>> some, Func<CancellationToken, Task<TResult>> none,
        CancellationToken cancellationToken)
    {
        return this switch
        {
            Some<TValue> someOption => await some(someOption.Value, cancellationToken),
            None<TValue> => await none(cancellationToken),
            _ => throw new OptionNotPresentException()
        };
    }

    /// <inheritdoc />
    [Pure]
    public override bool Equals(object? obj)
    {
        return obj is Option<TValue> other && Equals(other);
    }

    /// <inheritdoc />
    [Pure]
    public override int GetHashCode()
    {
        return HasValue ? HashCode.Combine(true, Value) : HashCode.Combine(false);
    }

    /// <inheritdoc />
    [Pure]
    public override string ToString()
    {
        return HasValue ? $"Some({Value})" : "None";
    }

    /// <summary>
    /// Creates an option that contains a value.
    /// </summary>
    /// <param name="value">The value to be contained in the option.</param>
    /// <returns>An option containing the provided value.</returns>
    [Pure]
    public static Option<TValue> Some([DisallowNull] TValue value)
    {
        return new Some<TValue>(value);
    }

    /// <summary>
    /// Creates an option from a nullable value. If the value is null, it returns <see cref="None" />.
    /// </summary>
    /// <param name="value">The nullable value to be wrapped in an option.</param>
    /// <returns>An option that either contains the value or represents no value.</returns>
    [Pure]
    public static Option<TValue> FromNullable(TValue? value)
    {
        return value == null ? None : Some(value);
    }

    /// <summary>
    /// Implicit conversion from a nullable value to an <see cref="Option{TValue}" />.
    /// </summary>
    /// <param name="value">The nullable value to convert.</param>
    /// <returns>A <see cref="None" /> if TValue is null else a <see cref="Some" /> containing the input value</returns>
    [Pure]
    public static implicit operator Option<TValue>(TValue? value)
    {
        return FromNullable(value);
    }
}
