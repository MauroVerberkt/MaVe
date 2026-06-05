namespace MaVe.Monads;

/// <summary>
/// Represents an exception thrown when an option does not contain a value.
/// </summary>
public class OptionNotPresentException() : InvalidOperationException("The option does not contain a value.");
