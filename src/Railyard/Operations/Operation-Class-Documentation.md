# `Operation<TInput>` Class Documentation

The `Operation<TInput>` class is an abstract base class for creating executable operations with input validation using functional programming patterns (Result monad). It provides a structured pipeline: **Parse → Validate → Execute**.

## Type Parameters

- `TInput`: The input type for the operation. Must be a reference type (`class`), typically a `record`.

## Class Inheritance

- `Operation<TInput>` implements `IOperation`

## Properties

### `HasInput`

```csharp
protected virtual bool HasInput => true;
```

Indicates whether this operation requires input parameters. Override to `false` for parameterless operations.

## Constructor

```csharp
protected Operation()
```

Validates that `TInput` is a record type. Throws `Exception` if not.

## Methods

### `Perform(string inputParameters)`

```csharp
public Result<string> Perform(string inputParameters)
```

Entry point that executes the operation pipeline:

1. Parses JSON input into `TInput`
2. Validates the parsed input
3. Executes the operation logic

Uses monadic binding (`BindWithData`, `BindAndTransform`) to chain operations and propagate errors.

**Returns:** `Result<string>` - Success with result message, or Failure with error details.

### `Validate(TInput input)` (abstract)

```csharp
protected abstract Result<TInput> Validate(TInput input);
```

Implement validation logic. Return `Result.Success(input)` or `Result.Failure<TInput>(exception)`.

### `Execute(TInput input)` (abstract)

```csharp
protected abstract Result<string> Execute(TInput input);
```

Implement the operation's core logic. Receives validated input.

## Example Usage

```csharp
internal class CalculateOperation : Operation<CalculateOperation.Input>
{
    private const string Name = nameof(CalculateOperation);
    private const string Info = "Performs a calculation with two numbers.";

    protected override Result<Input> Validate(Input input)
    {
        if (input.Divisor == 0)
            return Result.Failure<Input>(new ArgumentException("Divisor cannot be zero"));
        return Result.Success(input);
    }

    protected override Result<string> Execute(Input input)
    {
        var result = input.Value / input.Divisor;
        return Result.Success($"Result: {result}");
    }

    internal record Input(int Value, int Divisor);
}
```

## Related Types

- [`IOperation`](IOperation.cs) - Interface defining the `Perform` contract
- [`Result<T>`](../../HelperMonads/Result/Result.cs) - Monad for railway-oriented programming
- [`Yard`](../Yard.cs) - Discovers and resolves operations via DI
