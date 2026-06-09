namespace MaVe.Monads.UnitTests;

/// <summary>
/// Contains unit tests for the <c>Equals</c> method of the <see cref="Option{T}" /> class.
/// <para>
/// These tests validate the equality logic of <c>Option{T}</c> objects under different scenarios, including:
/// <list type="bullet">
///     <item>
///         <description>Equal options (Some with the same value, or both None)</description>
///     </item>
///     <item>
///         <description>Different options (Some vs. None, different values)</description>
///     </item>
///     <item>
///         <description>Comparisons with objects of different types</description>
///     </item>
///     <item>
///         <description>GetHashCode consistency with equality</description>
///     </item>
/// </list>
/// </para>
/// </summary>
[TestFixture]
public class OptionEqualsTests
{
    private const string TestValue = "hello";
    private const string OtherValue = "world";

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(Option{T})" /> returns <c>true</c> when both are Some with the same value.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenBothAreSomeWithSameValue()
    {
        // Arrange
        var option1 = Option<string>.Some(TestValue);
        var option2 = Option<string>.Some(TestValue);

        // Act
        var isEqual = option1.Equals(option2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(Option{T})" /> returns <c>false</c> when both are Some with different values.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenBothAreSomeWithDifferentValues()
    {
        // Arrange
        var option1 = Option<string>.Some(TestValue);
        var option2 = Option<string>.Some(OtherValue);

        // Act
        var isEqual = option1.Equals(option2);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(Option{T})" /> returns <c>true</c> when both are None.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenBothAreNone()
    {
        // Arrange
        var option1 = Option<string>.None;
        var option2 = Option<string>.None;

        // Act
        var isEqual = option1.Equals(option2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(Option{T})" /> returns <c>false</c> when one is Some and the other is None.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenOneIsSomeAndOtherIsNone()
    {
        // Arrange
        var some = Option<string>.Some(TestValue);
        var none = Option<string>.None;

        // Act & Assert
        Assert.That(some.Equals(none), Is.False);
        Assert.That(none.Equals(some), Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(object)" /> returns <c>true</c> when comparing an object to itself.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenObjectsAreSameInstance()
    {
        // Arrange
        var option = Option<string>.Some(TestValue);

        // Act
        var isEqual = option.Equals((object)option);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(object)" /> returns <c>false</c> when comparing with an object of a different
    /// type.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        // Arrange
        var option = Option<string>.Some(TestValue);
        var other = new object();

        // Act
        var isEqual = option.Equals(other);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.Equals(Option{T})" /> returns <c>false</c> when comparing with null.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        // Arrange
        var option = Option<string>.Some(TestValue);

        // Act
        var isEqual = option.Equals(null);

        // Assert
        Assert.That(isEqual, Is.False);
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.GetHashCode" /> returns the same value for equal options.
    /// </summary>
    [Test]
    public void GetHashCode_ShouldBeSame_WhenOptionsAreEqual()
    {
        // Arrange
        var option1 = Option<string>.Some(TestValue);
        var option2 = Option<string>.Some(TestValue);

        // Act & Assert
        Assert.That(option1.GetHashCode(), Is.EqualTo(option2.GetHashCode()));
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.GetHashCode" /> returns the same value for two None instances.
    /// </summary>
    [Test]
    public void GetHashCode_ShouldBeSame_WhenBothAreNone()
    {
        // Arrange
        var none1 = Option<string>.None;
        var none2 = Option<int>.None;

        // Act & Assert
        Assert.That(none1.GetHashCode(), Is.EqualTo(none2.GetHashCode()));
    }

    /// <summary>
    /// Tests that <see cref="Option{T}.GetHashCode" /> returns different values for Some and None.
    /// </summary>
    [Test]
    public void GetHashCode_ShouldDiffer_WhenOneIsSomeAndOtherIsNone()
    {
        // Arrange
        var some = Option<string>.Some(TestValue);
        var none = Option<string>.None;

        // Act & Assert
        Assert.That(some.GetHashCode(), Is.Not.EqualTo(none.GetHashCode()));
    }

    /// <summary>
    /// Tests that equal options work correctly as dictionary keys.
    /// </summary>
    [Test]
    public void Option_ShouldWorkCorrectly_AsDictionaryKey()
    {
        // Arrange
        var dictionary = new Dictionary<Option<string>, int>();
        var key = Option<string>.Some(TestValue);

        // Act
        dictionary[key] = 42;

        // Assert
        var lookupKey = Option<string>.Some(TestValue);
        Assert.That(dictionary.ContainsKey(lookupKey), Is.True);
        Assert.That(dictionary[lookupKey], Is.EqualTo(42));
    }

    /// <summary>
    /// Tests equality with value types to ensure the constraint works correctly.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenBothAreSomeWithSameValueType()
    {
        // Arrange
        var option1 = Option<int>.Some(42);
        var option2 = Option<int>.Some(42);

        // Act
        var isEqual = option1.Equals(option2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    /// <summary>
    /// Tests that None created via different paths are still equal.
    /// </summary>
    [Test]
    public void Equals_ShouldReturnTrue_WhenNoneCreatedViaDifferentPaths()
    {
        // Arrange
        var none1 = Option<string>.None;
        Option<string> none2 = (string?)null!;

        // Act
        var isEqual = none1.Equals(none2);

        // Assert
        Assert.That(isEqual, Is.True);
    }

    [Test]
    public void OperatorEquals_ShouldReturnTrue_WhenBothAreEqual()
    {
        var left = Option<string>.Some(TestValue);
        var right = Option<string>.Some(TestValue);

#pragma warning disable NUnit2010
        Assert.That(left == right, Is.True);
#pragma warning restore NUnit2010
    }

    [Test]
    public void OperatorEquals_ShouldReturnFalse_WhenDifferent()
    {
        var left = Option<string>.Some(TestValue);
        var right = Option<string>.Some(OtherValue);

#pragma warning disable NUnit2010
        Assert.That(left == right, Is.False);
#pragma warning restore NUnit2010
    }

    [Test]
    public void OperatorEquals_ShouldHandleNulls()
    {
        Option<string>? left = null;
        Option<string>? right = null;
        var value = Option<string>.Some(TestValue);

#pragma warning disable NUnit2010
        Assert.Multiple(() =>
        {
            Assert.That(left == right, Is.True);
            Assert.That(left == value, Is.False);
            Assert.That(value == left, Is.False);
        });
#pragma warning restore NUnit2010
    }

    [Test]
    public void OperatorNotEquals_ShouldReturnOppositeOfEquals()
    {
        var left = Option<string>.Some(TestValue);
        var same = Option<string>.Some(TestValue);
        var different = Option<string>.Some(OtherValue);

#pragma warning disable NUnit2010
        Assert.Multiple(() =>
        {
            Assert.That(left != same, Is.False);
            Assert.That(left != different, Is.True);
        });
#pragma warning restore NUnit2010
    }
}
