using System.Reflection;
using System.Text.Json;
using MaVe.Monads;

namespace MaVe.Railyard.Operations;

/// <inheritdoc />
internal abstract class Operation<TInput> : IOperation where TInput : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Operation{TInput}" /> class.
    /// </summary>
    /// <exception cref="Exception">Thrown when <typeparamref name="TInput" /> is not a record type.</exception>
    protected Operation()
    {
        if (!IsRecord(typeof(TInput)))
        {
            throw new Exception($"{nameof(TInput)} is not a record.");
        }
    }

    /// <summary>
    /// Gets a value indicating whether this operation requires an input parameter.
    /// The default implementation returns true, but can be overridden in derived classes.
    /// </summary>
    protected virtual bool HasInput => true;

    /// <inheritdoc />
    public Result<string> Perform(string inputParameters)
    {
        if (HasInput)
        {
            return ParseInput(inputParameters)
                .Bind(Validate)
                .Bind(Execute);
        }

        return Execute(default!); // We can use null suppression here since a record always has a default.
    }

    /// <summary>
    /// Validates the provided input.
    /// </summary>
    /// <param name="input">The input to be validated.</param>
    /// <returns>An <see cref="Result{TData}" /> representing the validation result.</returns>
    protected abstract Result<TInput> Validate(TInput input);

    /// <summary>
    /// Executes the operation using the validated input.
    /// </summary>
    /// <param name="input">The validated input to be used in the execution.</param>
    /// <returns>
    /// An <see cref="Result{TData}" /> where TData is a <see cref="string" />, representing the result of the execution.
    /// </returns>
    protected abstract Result<string> Execute(TInput input);

    /// <summary>
    /// Parses the input string into an object of type <typeparamref name="TInput" />.
    /// </summary>
    /// <param name="input">The input string in JSON format.</param>
    /// <returns>
    /// An <see cref="Result{TData}" /> representing the parsed input.
    /// </returns>
    private Result<TInput> ParseInput(string input)
    {
        try
        {
            var deserializeInput = JsonSerializer.Deserialize<TInput>(input);
            return deserializeInput == null
                ? Result.Failure<TInput>(RailyardErrors.InvalidInput<TInput>())
                : Result.Success(deserializeInput);
        }
        catch (Exception exception)
        {
            return Result.Failure<TInput>(Error.Unexpected(exception));
        }
    }

    /// <summary>
    /// Determines whether the specified type is a record type.
    /// </summary>
    /// <param name="type">The type to be checked.</param>
    /// <returns><c>true</c> if the type is a record; otherwise, <c>false</c>.</returns>
    private static bool IsRecord(Type type)
    {
        // Records are always classes with a constructor that has parameters
        return type.IsClass &&
               type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                   .Any(constructor => constructor.GetParameters().Length > 0);
    }
}
