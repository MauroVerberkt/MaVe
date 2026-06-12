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

    [Operation("echo")]
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

    [Operation("non-serializable")]
    private sealed class NonSerializableOperation : Operation<NonSerializableInput, NonSerializableOutput>
    {
        protected override Task<Result<NonSerializableOutput>> ExecuteAsync(NonSerializableInput input,
            CancellationToken ct)
        {
            return Task.FromResult(Result.Success(new NonSerializableOutput()));
        }
    }

    private sealed class EchoInput
    {
        public string Message { get; set; } = string.Empty;
    }

    private sealed class EchoOutput
    {
        public EchoOutput(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    private sealed class NonSerializableInput
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class NonSerializableOutput
    {
        public Stream Stream { get; } = Stream.Null;
    }
}
