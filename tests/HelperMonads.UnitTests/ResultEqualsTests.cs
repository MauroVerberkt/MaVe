namespace HelperMonads.UnitTests;

/// <summary>
/// Contains unit tests for the <c>Equals</c> method of the <see cref="Result{T}" /> class.
/// <para>
/// These tests validate the equality logic of <c>Result{T}</c> objects under different scenarios, including:
/// <list type="bullet">
///     <item>
///         <description>Equal results (success or failure with the same data or error)</description>
///     </item>
///     <item>
///         <description>Different results (success vs. failure, different data or exceptions)</description>
///     </item>
///     <item>
///         <description>Comparisons with objects of different types</description>
///     </item>
/// </list>
/// </para>
/// <para>
/// The tests ensure that the equality checks function correctly when comparing both successful and failure results with or
/// without data.
/// </para>
/// </summary>
[TestFixture]
public class ResultEqualsTests
{
    private static Error TestError => Error.Create(FailureMessage);
    private const string FailureMessage = "Failed";
    private const string SuccessMessage = "Success";
    private const string NextMessage = "Next";

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>true</c> when the results are equal.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenResultsAreEqual()
    {
        // Arrange
        var result1 = Result<string>.Success(SuccessMessage);
        object result2 = Result<string>.Success(SuccessMessage);

        // Act
        var isEqual = result1.Equals(result2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>false</c> when the results are not equal.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenResultsAreNotEqual()
    {
        // Arrange
        var result1 = Result<string>.Success(SuccessMessage);
        var result2 = Result<string>.Failure(TestError);

        // Act
        var isEqual = result1.Equals(result2);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>true</c> when comparing an object to itself (same
    /// instance).
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenObjectsAreSameInstance()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);

        // Act
        var isEqual = result.Equals(result);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>false</c> when comparing with an object of a different
    /// type.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        var result = Result<string>.Success(SuccessMessage);
        var other = new object(); // Different type

        // Act
        var isEqual = result.Equals(other);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>false</c> when the results have different data.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenResultsHaveDifferentData()
    {
        // Arrange
        var result1 = Result<string>.Success(SuccessMessage);
        var result2 = Result<string>.Success(NextMessage);

        // Act
        var isEqual = result1.Equals(result2);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>false</c> when one result is a success and the other is a
    /// failure.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenOneIsSuccessAndTheOtherIsFailure()
    {
        // Arrange
        var successResult = Result<string>.Success(SuccessMessage);
        var failureResult = Result<string>.Failure(TestError);

        // Act
        var isEqual = successResult.Equals(failureResult);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>true</c> when both results are failures with the same
    /// error.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenBothResultsAreFailureWithSameError()
    {
        var error = TestError;

        // Arrange
        var failureResult1 = Result<string>.Failure(error);
        var failureResult2 = Result<string>.Failure(error);

        // Act
        var isEqual = failureResult1.Equals(failureResult2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>true</c> when failure results have equal errors
    /// (value equality via record semantics).
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenFailureResultsHaveEqualErrors()
    {
        // Arrange
        var failureResult1 = Result<string>.Failure(Error.Create(FailureMessage));
        var failureResult2 = Result<string>.Failure(Error.Create(FailureMessage));

        // Act
        var isEqual = failureResult1.Equals(failureResult2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Result{T}.Equals(object)" /> returns <c>false</c> when failure results have different errors.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenFailureResultsHaveDifferentErrors()
    {
        // Arrange
        var failureResult1 = Result<string>.Failure(Error.Create(FailureMessage, "CODE_A"));
        var failureResult2 = Result<string>.Failure(Error.Create(FailureMessage, "CODE_B"));

        // Act
        var isEqual = failureResult1.Equals(failureResult2);

        // Assert
        Assert.That(isEqual, Is.False);
    }
}
