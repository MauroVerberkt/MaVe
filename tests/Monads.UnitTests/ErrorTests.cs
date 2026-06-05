namespace MaVe.Monads.UnitTests;

/// <summary>
/// Contains unit tests for the <see cref="Error" /> record type.
/// </summary>
[TestFixture]
public class ErrorTests
{
    private const string TestMessage = "Something went wrong";
    private const string TestCode = "ERR_001";

    [Test]
    public void Create_ShouldSetMessageAndCode()
    {
        // Act
        var error = Error.Create(TestMessage, TestCode);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo(TestMessage));
            Assert.That(error.Code, Is.EqualTo(TestCode));
            Assert.That(error.Exception, Is.Null);
        });
    }

    [Test]
    public void Create_WithNullCode_ShouldDefaultToNull()
    {
        // Act
        var error = Error.Create(TestMessage);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo(TestMessage));
            Assert.That(error.Code, Is.Null);
            Assert.That(error.Exception, Is.Null);
        });
    }

    [Test]
    public void Unexpected_ShouldCaptureException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var error = Error.Unexpected(exception);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Is.EqualTo("Test exception"));
            Assert.That(error.Code, Is.EqualTo("UNEXPECTED"));
            Assert.That(error.Exception, Is.SameAs(exception));
        });
    }

    [Test]
    public void Unexpected_WithNullException_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = Error.Unexpected(null!));
    }

    [Test]
    public void Equality_SameMessageAndCode_ShouldBeEqual()
    {
        // Arrange
        var error1 = Error.Create(TestMessage, TestCode);
        var error2 = Error.Create(TestMessage, TestCode);

        // Assert
        Assert.That(error1, Is.EqualTo(error2));
    }

    [Test]
    public void Equality_DifferentMessages_ShouldNotBeEqual()
    {
        // Arrange
        var error1 = Error.Create("Message A", TestCode);
        var error2 = Error.Create("Message B", TestCode);

        // Assert
        Assert.That(error1, Is.Not.EqualTo(error2));
    }

    [Test]
    public void Equality_DifferentCodes_ShouldNotBeEqual()
    {
        // Arrange
        var error1 = Error.Create(TestMessage, "CODE_A");
        var error2 = Error.Create(TestMessage, "CODE_B");

        // Assert
        Assert.That(error1, Is.Not.EqualTo(error2));
    }

    [Test]
    public void Equality_SameExceptionReference_ShouldBeEqual()
    {
        // Arrange
        var exception = new InvalidOperationException("test");
        var error1 = Error.Unexpected(exception);
        var error2 = Error.Unexpected(exception);

        // Assert
        Assert.That(error1, Is.EqualTo(error2));
    }

    [Test]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = Error.Create(TestMessage, TestCode);

        // Act
        var modified = original with { Code = "NEW_CODE" };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(modified.Message, Is.EqualTo(TestMessage));
            Assert.That(modified.Code, Is.EqualTo("NEW_CODE"));
            Assert.That(original.Code, Is.EqualTo(TestCode));
        });
    }

    [Test]
    public void ToString_ShouldBeReadable()
    {
        // Arrange
        var error = Error.Create(TestMessage, TestCode);

        // Act
        var result = error.ToString();

        // Assert
        Assert.That(result, Does.Contain(TestMessage));
        Assert.That(result, Does.Contain(TestCode));
    }

    [Test]
    public void GetHashCode_ShouldBeSameForEqualErrors()
    {
        // Arrange
        var error1 = Error.Create(TestMessage, TestCode);
        var error2 = Error.Create(TestMessage, TestCode);

        // Assert
        Assert.That(error1.GetHashCode(), Is.EqualTo(error2.GetHashCode()));
    }
}
