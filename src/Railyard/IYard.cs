using MaVe.Monads;
using MaVe.Railyard.Operations;

namespace MaVe.Railyard;

/// <summary>
/// Defines the contract for a yard that can retrieve operations by name
/// and provide additional operation information.
/// </summary>
public interface IYard
{
    /// <summary>
    /// Gets a dictionary containing operation names and their corresponding info.
    /// </summary>
    public InfoDictionary OperationsInfo { get; }

    /// <summary>
    /// Retrieves an operation by its name.
    /// </summary>
    /// <param name="operationName">The name of the operation to retrieve.</param>
    /// <returns>An <see cref="Option{IOperation}" /> containing the operation, or none if not found.</returns>
    public Option<IOperation> GetOperationByName(string operationName);
}
