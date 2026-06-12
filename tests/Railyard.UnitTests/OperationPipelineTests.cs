using MaVe.Monads;

namespace MaVe.Railyard.UnitTests;

[TestFixture]
public class OperationPipelineTests
{
    [Test]
    public async Task PerformAsync_WhenInputIsValid_ReturnsSerializedOutput()
    {
        var operation = new EchoOperation();

        var result = await operation.PerformAsync("{\"Message\":\"hello\"}", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo("{\"Message\":\"hello\"}"));
        });
    }

    [Test]
    public async Task PerformAsync_WhenJsonIsInvalid_ReturnsInvalidInputError()
    {
        var operation = new EchoOperation();

        var result = await operation.PerformAsync("{invalid-json", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Code, Is.EqualTo("RY001"));
            Assert.That(result.Error?.Message, Does.StartWith("Input could not be deserialized."));
        });
    }

    [Test]
    public async Task PerformAsync_WhenInputIsNullJson_ReturnsInputMustNotBeNullError()
    {
        var operation = new EchoOperation();

        var result = await operation.PerformAsync("null", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Code, Is.EqualTo("RY001"));
            Assert.That(result.Error?.Message, Is.EqualTo("Input must not be null."));
        });
    }

    [Test]
    public async Task PerformAsync_WhenValidationFails_ReturnsValidationError()
    {
        var operation = new EchoOperation();

        var result = await operation.PerformAsync("{\"Message\":\"\"}", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Message, Is.EqualTo("Message is required"));
        });
    }

    [Test]
    public async Task PerformAsync_WhenOutputSerializationFails_ReturnsSerializationError()
    {
        var operation = new NonSerializableOperation();

        var result =
            await operation.PerformAsync("{\"value\":\"x\"}", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Code, Is.EqualTo("RY003"));
        });
    }

    [Test]
    public async Task PerformAsync_WhenSyncOperationIsValid_ReturnsSerializedOutput()
    {
        var operation = new SyncEchoOperation();

        var result = await operation.PerformAsync("{\"Message\":\"hello\"}", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo("{\"Message\":\"hello\"}"));
        });
    }

    private sealed class EchoOperation : Operation<EchoInput, EchoOutput>
    {
        protected override Result<EchoInput> Validate(EchoInput input)
        {
            return string.IsNullOrWhiteSpace(input.Message)
                ? Result.Failure<EchoInput>(Error.Create("Message is required"))
                : Result.Success(input);
        }

        protected override Task<Result<EchoOutput>> ExecuteAsync(EchoInput input, CancellationToken ct)
        {
            return Task.FromResult(Result.Success(new EchoOutput(input.Message)));
        }
    }

    private sealed class NonSerializableOperation : Operation<NonSerializableInput, NonSerializableOutput>
    {
        protected override Task<Result<NonSerializableOutput>> ExecuteAsync(NonSerializableInput input,
            CancellationToken ct)
        {
            return Task.FromResult(Result.Success(new NonSerializableOutput()));
        }
    }

    private sealed class SyncEchoOperation : SyncOperation<EchoInput, EchoOutput>
    {
        protected override Result<EchoOutput> Execute(EchoInput input)
        {
            return Result.Success(new EchoOutput(input.Message));
        }
    }

    private sealed record EchoInput(string Message);

    private sealed class EchoOutput
    {
        public EchoOutput(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    private sealed record NonSerializableInput(string Value);

    private sealed class NonSerializableOutput
    {
        public Stream Stream { get; } = Stream.Null;
    }
}
