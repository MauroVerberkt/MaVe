using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace MaVe.Monads;

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
    /// <exception cref="OptionIsNoneException">
    /// Thrown if the option is in an invalid state and neither a <see cref="Some{TValue}" /> nor <see cref="None{TValue}" />.
    /// </exception>
    public TResult Match<TResult>(Func<TValue, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return this switch
        {
            Some<TValue> someOption => some(someOption.Value),
            None<TValue> => none(),
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
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
    /// <exception cref="OptionIsNoneException">
    /// Thrown if the option is in an invalid state and neither a <see cref="Some{TValue}" /> nor <see cref="None{TValue}" />.
    /// </exception>
    public async Task<TResult> MatchAsync<TResult>(Func<TValue, Task<TResult>> some, Func<Task<TResult>> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return this switch
        {
            Some<TValue> someOption => await some(someOption.Value).ConfigureAwait(false),
            None<TValue> => await none().ConfigureAwait(false),
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <inheritdoc cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, Task{TResult}}, Func{Task{TResult}})" />
    public async Task<TResult> MatchAsync<TResult>(
        Func<TValue, CancellationToken, Task<TResult>> some, Func<CancellationToken, Task<TResult>> none,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return this switch
        {
            Some<TValue> someOption => await some(someOption.Value, cancellationToken).ConfigureAwait(false),
            None<TValue> => await none(cancellationToken).ConfigureAwait(false),
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <summary>
    /// Transforms the value if present; otherwise propagates <see cref="None" />.
    /// </summary>
    /// <typeparam name="TNewValue">The transformed value type.</typeparam>
    /// <param name="transform">The transform to apply when a value is present.</param>
    [Pure]
    public Option<TNewValue> Map<TNewValue>(Func<TValue, TNewValue> transform)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(transform);

        return this switch
        {
            Some<TValue> someOption => Option<TNewValue>.Some(transform(someOption.Value)),
            None<TValue> => Option<TNewValue>.None,
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <inheritdoc cref="Map" />
    public async Task<Option<TNewValue>> MapAsync<TNewValue>(Func<TValue, Task<TNewValue>> transform)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(transform);

        return this switch
        {
            Some<TValue> someOption => Option<TNewValue>.Some(await transform(someOption.Value).ConfigureAwait(false)),
            None<TValue> => Option<TNewValue>.None,
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <inheritdoc cref="Map" />
    public async Task<Option<TNewValue>> MapAsync<TNewValue>(
        Func<TValue, CancellationToken, Task<TNewValue>> transform,
        CancellationToken cancellationToken)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(transform);

        return this switch
        {
            Some<TValue> someOption =>
                Option<TNewValue>.Some(await transform(someOption.Value, cancellationToken).ConfigureAwait(false)),
            None<TValue> => Option<TNewValue>.None,
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <summary>
    /// Binds the value to another option-producing function if present; otherwise propagates <see cref="None" />.
    /// </summary>
    /// <typeparam name="TNewValue">The bound value type.</typeparam>
    /// <param name="function">The option-producing function to invoke when a value is present.</param>
    [Pure]
    public Option<TNewValue> Bind<TNewValue>(Func<TValue, Option<TNewValue>> function)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(function);

        return this switch
        {
            Some<TValue> someOption => function(someOption.Value),
            None<TValue> => Option<TNewValue>.None,
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <inheritdoc cref="Bind" />
    public async Task<Option<TNewValue>> BindAsync<TNewValue>(Func<TValue, Task<Option<TNewValue>>> function)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(function);

        return this switch
        {
            Some<TValue> someOption => await function(someOption.Value).ConfigureAwait(false),
            None<TValue> => Option<TNewValue>.None,
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <inheritdoc cref="Bind" />
    public async Task<Option<TNewValue>> BindAsync<TNewValue>(
        Func<TValue, CancellationToken, Task<Option<TNewValue>>> function,
        CancellationToken cancellationToken)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(function);

        return this switch
        {
            Some<TValue> someOption => await function(someOption.Value, cancellationToken).ConfigureAwait(false),
            None<TValue> => Option<TNewValue>.None,
            _ => throw new OptionIsNoneException(typeof(TValue).Name)
        };
    }

    /// <summary>
    /// Projects the value into a new form. Enables LINQ <c>select</c> syntax.
    /// </summary>
    /// <param name="selector">The projection function.</param>
    [Pure]
    public Option<TNewValue> Select<TNewValue>(Func<TValue, TNewValue> selector)
        where TNewValue : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        return Map(selector);
    }

    /// <summary>
    /// Projects the value into an intermediate option and applies a result selector.
    /// Enables LINQ <c>from x in ... from y in ... select ...</c> syntax.
    /// </summary>
    /// <param name="selector">The function that projects to an intermediate option.</param>
    /// <param name="resultSelector">The function that combines original and intermediate values.</param>
    [Pure]
    public Option<TResult> SelectMany<TNewValue, TResult>(
        Func<TValue, Option<TNewValue>> selector,
        Func<TValue, TNewValue, TResult> resultSelector)
        where TNewValue : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        if (this is not Some<TValue> someOption)
        {
            return this is None<TValue> ? Option<TResult>.None : throw new OptionIsNoneException(typeof(TValue).Name);
        }

        var intermediate = selector(someOption.Value);

        if (intermediate is not Some<TNewValue> intermediateSome)
        {
            return intermediate is None<TNewValue>
                ? Option<TResult>.None
                : throw new OptionIsNoneException(typeof(TNewValue).Name);
        }

        var result = resultSelector(someOption.Value, intermediateSome.Value);
        return Option<TResult>.Some(result);
    }

    /// <inheritdoc />
    [Pure]
    public override bool Equals(object? obj)
    {
        return obj is Option<TValue> other && Equals(other);
    }

    /// <summary>
    /// Determines whether two <see cref="Option{TValue}" /> instances are equal.
    /// </summary>
    [Pure]
    public static bool operator ==(Option<TValue>? left, Option<TValue>? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    /// <summary>
    /// Determines whether two <see cref="Option{TValue}" /> instances are not equal.
    /// </summary>
    [Pure]
    public static bool operator !=(Option<TValue>? left, Option<TValue>? right)
    {
        return !(left == right);
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
