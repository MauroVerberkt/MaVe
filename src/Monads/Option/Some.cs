using System.Diagnostics.CodeAnalysis;

namespace MaVe.Monads;

/// <summary>
/// Represents an option that contains a value.
/// </summary>
/// <typeparam name="TValue">The type of the value, which must be a reference type (class).</typeparam>
public sealed class Some<TValue> : Option<TValue> where TValue : notnull
{
    internal Some([DisallowNull] TValue value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets a value indicating that the option contains a value.
    /// </summary>
    public override bool HasValue => true;

    /// <summary>
    /// Gets the value contained within the option.
    /// </summary>
    public override TValue Value { get; }
}
