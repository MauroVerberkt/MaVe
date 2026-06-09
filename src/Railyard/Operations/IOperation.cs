using MaVe.Monads;

namespace MaVe.Railyard.Operations;

/// <summary>
/// An operation that can be performed.
/// </summary>
public interface IOperation
{
    /// <summary>
    /// Performs an operation using the provided input parameters.
    /// </summary>
    /// <param name="inputParameters">The input be processed.</param>
    /// <returns>A Result object indicating the outcome of the operation.</returns>
    public Result<string> Perform(string inputParameters);
}
