namespace MaVe.Monads.UnitTests;

/// <summary>
/// Contains unit tests for various binding operations on the <see cref="Result{T}" /> class.
/// <para>
/// These tests validate the behavior of <c>Then</c>, <c>ThenAsync</c>, <c>Bind</c>, and <c>BindAsync</c>
/// methods under different scenarios including success, failure, and cancellation.
/// </para>
/// The tests ensure that these methods handle the propagation of success and failure states correctly, as well as pass
/// data and
/// respect cancellation tokens.
/// </summary>
[TestFixture]
public class ResultBindingTests
{
    private static Error TestError => Error.Create(FailureMessage);
    private const string FailureMessage = "ValidData";
    private const string SuccessMessage = "Success";
    private const string NextMessage = "Next";
    private const string ProcessedMessage = "Processed";

    /// <summary>
    /// Tests that the Then method returns a failure result when the initial result is a failure.
    /// </summary>
    [Test]
    public void Then_ShouldReturnResult_WhenFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var result = failureResult.Then(() => Result<string>.Success(NextMessage));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that the Then method invokes the provided function and returns a success result when the initial result is
    /// successful.
    /// </summary>
    [Test]
    public void Then_ShouldInvokeFunction_WhenSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = successResult.Then(() => Result<string>.Success(NextMessage));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(NextMessage));
        });
    }

    [Test]
    public void Then_Generic_ShouldTransformType_WhenSuccess()
    {
        var successResult = Result<string>.Success(SuccessMessage);

        var result = successResult.Then(() => Result<int>.Success(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(1));
        });
    }

    [Test]
    public void Then_Generic_ShouldPropagateFailure_WhenFailure()
    {
        var failureResult = Result<string>.Failure(TestError);

        var result = failureResult.Then(() => Result<int>.Success(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that the Bind method returns a failure result when the initial result is a failure.
    /// </summary>
    [Test]
    public void Bind_ShouldReturnResult_WhenFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var result = failureResult.Bind(_ => Result<string>.Success(NextMessage));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that the Bind method invokes the provided function and returns a success result when the initial result
    /// is
    /// successful.
    /// </summary>
    [Test]
    public void Bind_ShouldInvokeFunction_WhenSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = successResult.Bind(data => Result<string>.Success(data + NextMessage));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(SuccessMessage + NextMessage));
        });
    }

    /// <summary>
    /// Tests that the BindAsync method returns a success result when the initial result is a success.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnSuccess_WhenResultIsSuccess()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);

        // Act
        var processedResult = await result.BindAsync(ProcessData);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(SuccessMessage + ProcessedMessage));
        });
        return;

        Task<Result<string>> ProcessData(string data)
        {
            return Task.FromResult(Result<string>.Success(data + ProcessedMessage));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method returns a failure result when the initial result is a failure.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnFailure_WhenResultIsFailure()
    {
        // Arrange
        var result = Result<string>.Failure(TestError);

        // Act
        var processedResult = await result.BindAsync(ProcessData);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
        return;

        Task<Result<string>> ProcessData(string data)
        {
            return Task.FromResult(Result<string>.Success(data + ProcessedMessage));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method passes data to the function when the initial result is a success.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldPassDataToFunction_WhenResultIsSuccess()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var processedResult = await result.BindAsync(AddDataToResult);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(15));
        });
        return;

        Task<Result<int>> AddDataToResult(int data)
        {
            return Task.FromResult(Result<int>.Success(data + 5));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method returns a failure result when the initial result is a failure, and a
    /// cancellation
    /// token is used.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnFailure_WhenResultIsFailure_WithCancellationToken()
    {
        // Arrange
        var result = Result<int>.Failure(TestError);
        var cancellationToken = CancellationToken.None;

        // Act
        var processedResult = await result.BindAsync(ProcessData, cancellationToken);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
        return;

        Task<Result<int>> ProcessData(int data, CancellationToken ct)
        {
            return Task.FromResult(Result<int>.Success(data + 10));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method returns a success result when the initial result is a success, and a
    /// cancellation
    /// token is used.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnSuccess_WhenResultIsSuccess_WithCancellationToken()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        var cancellationToken = CancellationToken.None;

        // Act
        var processedResult = await result.BindAsync(AppendDataToResult, cancellationToken);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(SuccessMessage + ProcessedMessage));
        });
        return;

        Task<Result<string>> AppendDataToResult(string data, CancellationToken ct)
        {
            return Task.FromResult(Result<string>.Success(data + ProcessedMessage));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method respects cancellation when the cancellation token is canceled.
    /// </summary>
    [Test]
    public void BindAsync_ShouldRespectCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            _ = await result.BindAsync(ProcessDataWithCancellation, cancellationTokenSource.Token));
        return;

        async Task<Result<string>> ProcessDataWithCancellation(string data, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<string>.Success(data + ProcessedMessage);
        }
    }

    /// <summary>
    /// Tests that the ThenAsync method returns a success result when the initial result is a success.
    /// </summary>
    [Test]
    public async Task ThenAsync_ShouldReturnSuccess_WhenResultIsSuccess()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);

        // Act
        var processedResult = await result.ThenAsync(ProcessDataAsync);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(SuccessMessage + ProcessedMessage));
        });
        return;

        Task<Result<string>> ProcessDataAsync()
        {
            return Task.FromResult(Result<string>.Success(SuccessMessage + ProcessedMessage));
        }
    }

    /// <summary>
    /// Tests that the ThenAsync method returns a failure result when the initial result is a failure.
    /// </summary>
    [Test]
    public async Task ThenAsync_ShouldReturnFailure_WhenResultIsFailure()
    {
        // Arrange
        var result = Result<string>.Failure(TestError);

        // Act
        var processedResult = await result.ThenAsync(ProcessDataAsync);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
        return;

        Task<Result<string>> ProcessDataAsync()
        {
            return Task.FromResult(Result<string>.Success(FailureMessage + ProcessedMessage));
        }
    }

    /// <summary>
    /// Tests that the ThenAsync method invokes the function when the initial result is a success.
    /// </summary>
    [Test]
    public async Task ThenAsync_ShouldInvokeFunction_WhenResultIsSuccess()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var processedResult = await result.ThenAsync(AddDataToResultAsync);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(15));
        });
        return;

        Task<Result<int>> AddDataToResultAsync()
        {
            return Task.FromResult(Result<int>.Success(10 + 5));
        }
    }

    /// <summary>
    /// Tests that the ThenAsync method returns a failure result when the initial result is a failure, and a cancellation token
    /// is
    /// used.
    /// </summary>
    [Test]
    public async Task ThenAsync_ShouldReturnFailure_WhenResultIsFailure_WithCancellationToken()
    {
        // Arrange
        var result = Result<int>.Failure(TestError);
        var cancellationToken = CancellationToken.None;

        // Act
        var processedResult = await result.ThenAsync(ProcessDataAsync, cancellationToken);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
        return;

        async Task<Result<int>> ProcessDataAsync(CancellationToken ct)
        {
            await Task.Delay(50, ct);
            return Result<int>.Success(10 + 5);
        }
    }

    /// <summary>
    /// Tests that the ThenAsync method returns a success result when the initial result is a success, and a cancellation token
    /// is
    /// used.
    /// </summary>
    [Test]
    public async Task ThenAsync_ShouldReturnSuccess_WhenResultIsSuccess_WithCancellationToken()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        var cancellationToken = CancellationToken.None;

        // Act
        var processedResult = await result.ThenAsync(AppendDataToResultAsync, cancellationToken);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(SuccessMessage + ProcessedMessage));
        });
        return;

        async Task<Result<string>> AppendDataToResultAsync(CancellationToken ct)
        {
            await Task.Delay(50, ct);
            return Result<string>.Success(SuccessMessage + ProcessedMessage);
        }
    }

    /// <summary>
    /// Tests that the ThenAsync method respects cancellation when the cancellation token is canceled.
    /// </summary>
    [Test]
    public void ThenAsync_ShouldRespectCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            _ = await result.ThenAsync(ProcessDataWithCancellationAsync, cancellationTokenSource.Token));
        return;

        async Task<Result<string>> ProcessDataWithCancellationAsync(CancellationToken ct)
        {
            await Task.Delay(100, ct);
            ct.ThrowIfCancellationRequested();
            return Result<string>.Success(SuccessMessage + ProcessedMessage);
        }
    }

    [Test]
    public async Task ThenAsync_Generic_ShouldTransformType_WhenSuccess()
    {
        var result = Result<string>.Success(SuccessMessage);

        var processedResult = await result.ThenAsync(() => Task.FromResult(Result<int>.Success(1)));

        Assert.Multiple(() =>
        {
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(1));
        });
    }

    [Test]
    public void ThenAsync_Generic_ShouldRespectCancellation_WhenTokenIsCancelled()
    {
        var result = Result<string>.Success(SuccessMessage);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            _ = await result.ThenAsync(async ct =>
            {
                await Task.Delay(100, ct);
                ct.ThrowIfCancellationRequested();
                return Result<int>.Success(1);
            }, cancellationTokenSource.Token));
    }

    [Test]
    public async Task ThenAsync_Generic_ShouldPropagateFailure_WhenFailure_WithCancellationToken()
    {
        var result = Result<string>.Failure(TestError);
        var cancellationToken = CancellationToken.None;

        var processedResult = await result.ThenAsync(
            _ => Task.FromResult(Result<int>.Success(1)),
            cancellationToken);

        Assert.Multiple(() =>
        {
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that the Bind method returns a failure result when the initial result is a failure while transforming types.
    /// </summary>
    [Test]
    public void Bind_ShouldReturnResult_WhenFailure_WithTypeTransform()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var result = failureResult.Bind(_ => Result<int>.Success(1));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that the Bind method invokes the provided function and returns a success result when the initial
    /// result is
    /// successful.
    /// </summary>
    [Test]
    public void Bind_ShouldInvokeFunction_WhenSuccess_WithTypeTransform()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = successResult.Bind(_ => Result<int>.Success(1));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Tests that the BindAsync method returns a success result when the initial result is a success while transforming
    /// types.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnSuccess_WhenResultIsSuccess_WithTypeTransform()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);

        // Act
        var processedResult = await result.BindAsync(ProcessData);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(1));
        });
        return;

        Task<Result<int>> ProcessData(string data)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method returns a failure result when the initial result is a failure while transforming
    /// types.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnFailure_WhenResultIsFailure_WithTypeTransform()
    {
        // Arrange
        var result = Result<string>.Failure(TestError);

        // Act
        var processedResult = await result.BindAsync(ProcessData);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
        return;

        Task<Result<int>> ProcessData(string data)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method passes data to the function when the initial result is a success while transforming
    /// types.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldPassDataToFunction_WhenResultIsSuccess_WithTypeTransform()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);

        // Act
        var processedResult = await result.BindAsync(AddDataToResult);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(1));
        });
        return;

        Task<Result<int>> AddDataToResult(string data)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method returns a failure result when the initial result is a failure, and a
    /// cancellation
    /// token is used.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnFailure_WhenResultIsFailure_WithCancellationToken_WithTypeTransform()
    {
        // Arrange
        var result = Result<string>.Failure(TestError);
        var cancellationToken = CancellationToken.None;

        // Act
        var processedResult = await result.BindAsync(ProcessData, cancellationToken);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsFailure, Is.True);
            Assert.That(processedResult.Error?.Message, Is.EqualTo(FailureMessage));
        });
        return;

        Task<Result<int>> ProcessData(string data, CancellationToken ct)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method returns a success result when the initial result is a success, and a
    /// cancellation
    /// token is used.
    /// </summary>
    [Test]
    public async Task BindAsync_ShouldReturnSuccess_WhenResultIsSuccess_WithCancellationToken_WithTypeTransform()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        var cancellationToken = CancellationToken.None;

        // Act
        var processedResult = await result.BindAsync(AppendDataToResult, cancellationToken);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(processedResult.IsSuccess, Is.True);
            Assert.That(processedResult.Data, Is.EqualTo(1));
        });
        return;

        Task<Result<int>> AppendDataToResult(string data, CancellationToken ct)
        {
            return Task.FromResult(Result<int>.Success(1));
        }
    }

    /// <summary>
    /// Tests that the BindAsync method respects cancellation when the cancellation token is canceled while transforming
    /// types.
    /// </summary>
    [Test]
    public void BindAsync_ShouldRespectCancellation_WhenTokenIsCancelled_WithTypeTransform()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            _ = await result.BindAsync(ProcessDataWithCancellation, cancellationTokenSource.Token));
        return;

        async Task<Result<int>> ProcessDataWithCancellation(string data, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<int>.Success(1);
        }
    }
}
