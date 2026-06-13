namespace MaVe.Monads.UnitTests;

/// <summary>
/// Contains unit tests for the <see cref="Result{TData}" /> class, which represents the result of an operation that may
/// either
/// succeed or fail.
/// <para>
/// These tests cover various methods and functionalities of the <see cref="Result{TData}" /> class, including success and
/// failure creation, data mapping (both synchronous and asynchronous), string representation, deconstruction, and behavior
/// when
/// exceptions occur.
/// </para>
/// The tests ensure that the class behaves as expected in different scenarios, such as success, failure, cancellation, and
/// transformation.
/// </summary>
[TestFixture]
public class ResultTests
{
    private static Error TestError => Error.Create(FailureMessage);
    private const string FailureMessage = "Failed";
    private const string SuccessMessage = "Success";
    private const string ProcessedMessage = "Processed";

    /// <summary>
    /// Tests that <see cref="Result.Success{TData}(TData)" /> correctly creates a success result with valid data.
    /// </summary>
    [Test]
    public void Success_ShouldCreateSuccessResult_WithSuccessMessage()
    {
        // Act
        var result = Result.Success(SuccessMessage);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(SuccessMessage));
            Assert.That(result.Error, Is.Null);
        });
    }

    /// <summary>
    /// Tests that <see cref="Result.Failure{TData}(Error)" /> correctly creates a failure result with only an error.
    /// </summary>
    [Test]
    public void Failure_ShouldCreateFailureResult_WithErrorOnly()
    {
        // Act
        var result = Result.Failure<string>(TestError);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Data, Is.Null);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Success(TData)" /> throws an <see cref="ArgumentNullException" /> when the data is
    /// null.
    /// </summary>
    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenSuccessAndDataIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => { _ = Result<string>.Success(null!); });
        Assert.That(ex?.Message, Contains.Substring("Data must be provided for a successful result."));
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Success(TData)" /> throws an <see cref="ArgumentNullException" /> when the data is
    /// null.
    /// </summary>
    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenFailureAndErrorIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => { _ = Result<string>.Failure(null!); });
        Assert.That(ex?.Message, Contains.Substring("Error must be provided for a failed result."));
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Map{TNewData}(Func{TData, TNewData})" /> correctly maps data when the result is a
    /// success.
    /// </summary>
    [Test]
    public void Map_ShouldReturnMappedData_WhenSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = successResult.Map(data => data.ToUpper());

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(SuccessMessage.ToUpper()));
        });
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Map{TNewData}(Func{TData, TNewData})" /> returns a failure result when the result
    /// is a
    /// failure.
    /// </summary>
    [Test]
    public void Map_ShouldReturnFailure_WhenFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var result = failureResult.Map(data => data.ToUpper());

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, Task{TNewData}})" /> correctly maps
    /// data asynchronously when the result is a success.
    /// </summary>
    [Test]
    public async Task MapAsync_ShouldReturnMappedData_WhenSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = await successResult.MapAsync(data => Task.FromResult(data.ToUpper()));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(SuccessMessage.ToUpper()));
        });
    }

    /// <summary>
    /// Tests that
    /// <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, CancellationToken, Task{TNewData}}, CancellationToken)" />
    /// returns a failure result asynchronously when the result is a failure.
    /// </summary>
    [Test]
    public async Task MapAsync_ShouldReturnFailure_WhenFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var result = await failureResult.MapAsync(data => Task.FromResult(data.ToUpper()));

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error?.Message, Is.EqualTo(FailureMessage));
        });
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.ToString()" /> correctly returns a string representation when the result is a
    /// success.
    /// </summary>
    [Test]
    public void ToString_ShouldReturnSuccess_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var resultString = successResult.ToString();

        // Assert
        Assert.That(resultString, Is.EqualTo($"Success: {SuccessMessage}"));
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.ToString()" /> correctly returns a string representation when the result is a
    /// failure.
    /// </summary>
    [Test]
    public void ToString_ShouldReturnFailure_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var resultString = failureResult.ToString();

        // Assert
        Assert.That(resultString, Is.EqualTo($"Failure: {FailureMessage}"));
    }

    /// <summary>
    /// Tests that the deconstruction of a result correctly returns the success status, data, and error.
    /// </summary>
    [Test]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var (isSuccess, data, error) = successResult;

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(isSuccess, Is.True);
            Assert.That(data, Is.EqualTo(SuccessMessage));
            Assert.That(error, Is.Null);
        });
    }

    /// <summary>
    /// Tests that
    /// <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, CancellationToken, Task{TNewData}}, CancellationToken)" />
    /// returns a
    /// failure result when the result is a failure and an asynchronous map operation is applied.
    /// </summary>
    [Test]
    public async Task MapAsync_ShouldReturnFailure_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var result = await failureResult.MapAsync((Func<string, CancellationToken, Task<int>>)Transform,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<int>>());
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.InstanceOf<Error>());
        });
        return;

        Task<int> Transform(string data, CancellationToken token)
        {
            return Task.FromResult(data.Length);
        }
    }

    /// <summary>
    /// Tests that
    /// <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, CancellationToken, Task{TNewData}}, CancellationToken)" />
    /// returns a
    /// success result when the result is a success and an asynchronous map operation is applied.
    /// </summary>
    [Test]
    public async Task MapAsync_ShouldReturnSuccess_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = await successResult.MapAsync((Func<string, CancellationToken, Task<int>>)Transform,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<int>>());
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(SuccessMessage.Length));
        });
        return;

        Task<int> Transform(string data, CancellationToken _)
        {
            return Task.FromResult(data.Length);
        }
    }

    /// <summary>
    /// Tests that
    /// <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, CancellationToken, Task{TNewData}}, CancellationToken)" />
    /// returns a
    /// success result when the result is a success and a different type of mapped data is returned asynchronously.
    /// </summary>
    [Test]
    public async Task MapAsync_ShouldReturnSuccess_WhenResultIsSuccess_WithDifferentMappedType()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = await successResult.MapAsync((Func<string, CancellationToken, Task<string>>)Transform,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<string>>());
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(ProcessedMessage));
        });
        return;

        Task<string> Transform(string data, CancellationToken token)
        {
            return Task.FromResult(ProcessedMessage);
        }
    }

    /// <summary>
    /// Tests that
    /// <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, CancellationToken, Task{TNewData}}, CancellationToken)" />
    /// respects
    /// cancellation when the operation is cancelled.
    /// </summary>
    [Test]
    public void MapAsync_ShouldRespectCancellation_WhenCancelled()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        Func<string, CancellationToken, Task<int>> transform = async (data, token) =>
        {
            await Task.Delay(1000, token);
            return data.Length;
        };

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            _ = await successResult.MapAsync(transform, cancellationTokenSource.Token);
        });
    }

    [Test]
    public void MapAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled_AndResultIsFailure()
    {
        var failureResult = Result<string>.Failure(TestError);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            _ = await failureResult.MapAsync((_, _) => Task.FromResult(1), cancellationTokenSource.Token));
    }

    /// <summary>
    /// Tests that
    /// <see cref="Result{TData}.MapAsync{TNewData}(Func{TData, CancellationToken, Task{TNewData}}, CancellationToken)" />
    /// correctly
    /// applies multiple transformations asynchronously to the data.
    /// </summary>
    [Test]
    public async Task MapAsync_ShouldReturnMappedData_WhenMultipleTransformations()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var result = await successResult.MapAsync((Func<string, CancellationToken, Task<string>>)Transform,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<string>>());
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(SuccessMessage.ToUpper()));
        });
        return;

        Task<string> Transform(string data, CancellationToken token)
        {
            return Task.FromResult(data.ToUpper());
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.OnSuccess(Action{TData})" /> executes the action when the result is a success.
    /// </summary>
    [Test]
    public void OnSuccess_ShouldExecuteAction_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);
        var actionExecuted = false;

        // Act
        successResult.OnSuccess(Action)
            .OnSuccess(Action);

        // Assert
        Assert.That(actionExecuted, Is.True);
        return;

        void Action(string data)
        {
            actionExecuted = true;
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.OnSuccess(Action{TData})" /> does not execute the action when the result is a
    /// failure.
    /// </summary>
    [Test]
    public void OnSuccess_ShouldNotExecuteAction_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);
        var actionExecuted = false;

        // Act
        failureResult.OnSuccess(Action);

        // Assert
        Assert.That(actionExecuted, Is.False);
        return;

        void Action(string data)
        {
            actionExecuted = true;
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.OnFailure(Action{Error})" /> executes the action when the result is a failure.
    /// </summary>
    [Test]
    public void OnFailure_ShouldExecuteAction_WhenResultIsFailure()
    {
        // Arrange
        var successResult = Result<string>.Failure(TestError);
        var actionExecuted = false;

        // Act
        successResult.OnFailure(Action)
            .OnFailure(Action);

        // Assert
        Assert.That(actionExecuted, Is.True);
        return;

        void Action(Error error)
        {
            actionExecuted = true;
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.OnFailure(Action{Error})" /> does not execute the action when the result is a
    /// success.
    /// </summary>
    [Test]
    public void OnFailure_ShouldNotExecuteAction_WhenResultIsSuccess()
    {
        // Arrange
        var failureResult = Result<string>.Success(SuccessMessage);
        var actionExecuted = false;

        // Act
        failureResult.OnFailure(Action);

        // Assert
        Assert.That(actionExecuted, Is.False);
        return;

        void Action(Error error)
        {
            actionExecuted = true;
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Tap(Action{Result{TData}})" /> executes the action when the result is a success.
    /// </summary>
    [Test]
    public void Tap_ShouldExecuteAction_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);
        var actionExecuted = false;

        // Act
        successResult.Tap(Action);

        // Assert
        Assert.That(actionExecuted, Is.True);
        return;

        void Action(Result<string> result)
        {
            actionExecuted = true;
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Tap(Action{Result{TData}})" /> executes the action when the result is a failure.
    /// </summary>
    [Test]
    public void Tap_ShouldExecuteAction_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<string>.Failure(TestError);
        var actionExecuted = false;

        // Act
        failureResult.Tap(Action);

        // Assert
        Assert.That(actionExecuted, Is.True);
        return;

        void Action(Result<string> result)
        {
            actionExecuted = true;
        }
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.Tap(Action{Result{TData}})" /> returns the same result instance.
    /// </summary>
    [Test]
    public void Tap_ShouldReturnSameResultInstance()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);

        // Act
        var returnedResult = successResult.Tap(_ => { });

        // Assert
        Assert.That(returnedResult, Is.SameAs(successResult));
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.GetHashCode()" /> returns the same hash code for equal results.
    /// </summary>
    [Test]
    public void GetHashCode_ShouldReturnSameHashCodeForEqualResults()
    {
        // Arrange
        var successResult1 = Result<string>.Success(SuccessMessage);
        var successResult2 = Result<string>.Success(SuccessMessage);

        // Act
        var hashCode1 = successResult1.GetHashCode();
        var hashCode2 = successResult2.GetHashCode();

        // Assert
        Assert.That(hashCode1, Is.EqualTo(hashCode2));
    }

    /// <summary>
    /// Tests that <see cref="Result{TData}.GetHashCode()" /> returns different hash codes for different results.
    /// </summary>
    [Test]
    public void GetHashCode_ShouldReturnDifferentHashCodesForDifferentResults()
    {
        // Arrange
        var successResult1 = Result<string>.Success(SuccessMessage);
        var successResult2 = Result<string>.Failure(TestError);

        // Act
        var hashCode1 = successResult1.GetHashCode();
        var hashCode2 = successResult2.GetHashCode();

        // Assert
        Assert.That(hashCode1, Is.Not.EqualTo(hashCode2));
    }
}
