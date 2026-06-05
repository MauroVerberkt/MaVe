using System.Diagnostics.CodeAnalysis;

namespace MaVe.Monads;

/// <summary>
/// Represents an option that contains no value.
/// </summary>
/// <typeparam name="TValue">The type of the value, which must be a reference type (class).</typeparam>
public sealed class None<TValue> : Option<TValue> where TValue : notnull
{
    internal None()
    {
    }

    /// <summary>
    /// Gets a value indicating that the option does not contain a value.
    /// </summary>
    public override bool HasValue => false;

    /// <summary>
    /// Throws an exception because the option does not contain a value.
    /// </summary>
    /// <exception cref="OptionIsNoneException">
    /// Thrown when trying to access the value of an option that has no value.
    /// </exception>
    public override TValue Value
    {
        [DoesNotReturn]
        get => throw new OptionIsNoneException(typeof(TValue).Name);
    }
}
