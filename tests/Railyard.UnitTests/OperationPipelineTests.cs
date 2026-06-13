using MaVe.Monads;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    [Test]
    public void PerformAsync_WhenExecutionIsCanceled_ThrowsOperationCanceledException()
    {
        var operation = new CancelingOperation();
        var ct = new CancellationToken(canceled: true);

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await operation.PerformAsync("{\"Message\":\"hello\"}", null, ct));
    }

    [Test]
    public async Task PerformAsync_WhenExecutionReturnsFailure_PropagatesExecutionError()
    {
        var operation = new FailingExecutionOperation();

        var result = await operation.PerformAsync("{\"Message\":\"hello\"}", null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Code, Is.EqualTo("RY900"));
            Assert.That(result.Error?.Message, Is.EqualTo("Execution failed"));
        });
    }

    [Test]
    public void PerformAsync_WhenDeserializeThrowsOperationCanceledException_Rethrows()
    {
        var operation = new EchoOperation();
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new ThrowOnReadEchoInputConverter());

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await operation.PerformAsync("{\"Message\":\"hello\"}", serializerOptions, CancellationToken.None));
    }

    [Test]
    public void PerformAsync_WhenSerializeThrowsOperationCanceledException_Rethrows()
    {
        var operation = new EchoOperation();
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new ThrowOnWriteEchoOutputConverter());

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await operation.PerformAsync("{\"Message\":\"hello\"}", serializerOptions, CancellationToken.None));
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

    private sealed class CancelingOperation : Operation<EchoInput, EchoOutput>
    {
        protected override Task<Result<EchoOutput>> ExecuteAsync(EchoInput input, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Result.Success(new EchoOutput(input.Message)));
        }
    }

    private sealed class FailingExecutionOperation : Operation<EchoInput, EchoOutput>
    {
        protected override Task<Result<EchoOutput>> ExecuteAsync(EchoInput input, CancellationToken ct)
        {
            return Task.FromResult(Result.Failure<EchoOutput>(Error.Create("Execution failed", "RY900")));
        }
    }

    private sealed class ThrowOnReadEchoInputConverter : JsonConverter<EchoInput>
    {
        public override EchoInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new OperationCanceledException();
        }

        public override void Write(Utf8JsonWriter writer, EchoInput value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(EchoInput.Message), value.Message);
            writer.WriteEndObject();
        }
    }

    private sealed class ThrowOnWriteEchoOutputConverter : JsonConverter<EchoOutput>
    {
        public override EchoOutput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new EchoOutput("unused");
        }

        public override void Write(Utf8JsonWriter writer, EchoOutput value, JsonSerializerOptions options)
        {
            throw new OperationCanceledException();
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
