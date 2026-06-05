namespace MaVe.Monads;

/// <summary>
/// Represents an exception thrown when an operation is attempted on an option that has no value.
/// </summary>
/// <param name="typeName">The name of the type that was expected to contain a value.</param>
public class OptionIsNoneException(string typeName)
    : InvalidOperationException($"No value present for type {typeName}. This Option is None.");
