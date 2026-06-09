using MaVe.Monads;

namespace MaVe.Railyard.Operations;

/// <summary>
/// Contains domain-specific errors used by the Railyard library.
/// </summary>
public static class RailyardErrors
{
    private const string InvalidInputCode = "RY001";

    /// <summary>
    /// Creates an error indicating that the provided input could not be recognized
    /// as the expected type.
    /// </summary>
    /// <typeparam name="TExpected">
    /// The expected input type.
    /// </typeparam>
    /// <returns>
    /// An <see cref="Error" /> with code <c>RY001</c>.
    /// </returns>
    public static Error InvalidInput<TExpected>()
    {
        return Error.Create($"The input was not recognized as a '{typeof(TExpected).Name}'.", InvalidInputCode);
    }
}
