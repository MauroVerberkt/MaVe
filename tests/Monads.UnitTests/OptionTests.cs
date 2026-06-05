using Moq;

namespace MaVe.Monads.UnitTests;

/// <summary>
/// Tests for the <see cref="Option{TValue}" /> class, which represents an optional value that may or may not be present.
/// <para>
/// These tests cover various methods of the <see cref="Option{TValue}" /> class, including handling of
/// <see cref="Some{TValue}" /> and <see cref="None{TValue}" /> values, async and sync match operations, implicit
/// conversion,
/// and string representation.
/// </para>
/// </summary>
[TestFixture]
public class OptionTests
{
    private const string ValidValue = "TestValue";
    private const string? NullValue = null;
    private const string NoValueString = "No Value";

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.Some" /> method creates an option that contains a value.
    /// </summary>
    [Test]
    public void Some_ShouldContainValue()
    {
        // Arrange
        var option = Option<string>.Some(ValidValue);

        // Act
        var hasValue = option.HasValue;
        var optionValue = option.Value;

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(hasValue, Is.True);
            Assert.That(optionValue, Is.EqualTo(ValidValue));
        });
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.None" /> method creates an option that does not contain a value.
    /// </summary>
    [Test]
    public void None_ShouldNotContainValue()
    {
        // Arrange
        var option = Option<string>.None;

        // Act
        var hasValue = option.HasValue;

        // Assert
        Assert.That(hasValue, Is.False);
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.Match{TResult}(Func{TValue,TResult}, Func{TResult})" /> method returns the
    /// correct
    /// value when the option contains a value (Some).
    /// </summary>
    [Test]
    public void Match_ShouldReturnSome_WhenOptionIsSome()
    {
        // Arrange
        var option = Option<string>.Some(ValidValue);

        // Act
        var result = option.Match(SomeFunc, NoneFunc);

        // Assert
        Assert.That(result, Is.EqualTo(ValidValue));
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.Match{TResult}(Func{TValue, TResult}, Func{TResult})" /> method returns the
    /// correct
    /// value when the option does not contain a value
    /// (None).
    /// </summary>
    [Test]
    public void Match_ShouldReturnNone_WhenOptionIsNone()
    {
        // Arrange
        var option = Option<string>.None;

        // Act
        var result = option.Match(SomeFunc, NoneFunc);

        // Assert\
        Assert.That(result, Is.EqualTo(NoValueString));
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, Task{TResult}}, Func{Task{TResult}})" />
    /// method
    /// asynchronously returns the correct value when the option
    /// contains
    /// a value (Some).
    /// </summary>
    [Test]
    public async Task MatchAsync_ShouldReturnSome_WhenOptionIsSome()
    {
        // Arrange
        var option = Option<string>.Some(ValidValue);

        // Act
        var result = await option.MatchAsync(AsyncSomeFunc, AsyncNoneFunc);

        // Assert
        Assert.That(result, Is.EqualTo(ValidValue));
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, Task{TResult}}, Func{Task{TResult}})" />
    /// method
    /// asynchronously returns the correct value when the option does
    /// not
    /// contain a value (None).
    /// </summary>
    [Test]
    public async Task MatchAsync_ShouldReturnNone_WhenOptionIsNone()
    {
        // Arrange
        var option = Option<string>.None;

        // Act
        var result = await option.MatchAsync(AsyncSomeFunc, AsyncNoneFunc);

        // Assert
        Assert.That(result, Is.EqualTo(NoValueString));
    }

    /// <summary>
    /// Tests that the
    /// <see
    ///     cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, CancellationToken, Task{TResult}}, Func{CancellationToken, Task{TResult}}, CancellationToken)" />
    /// method asynchronously returns the correct value when the option
    /// contains
    /// a value (Some) with a cancellation token.
    /// </summary>
    [Test]
    public async Task MatchAsync_WithCancellation_ShouldReturnSome_WhenOptionIsSome()
    {
        // Arrange
        var option = Option<string>.Some(ValidValue);

        // Act
        var result = await option.MatchAsync(AsyncSomeFunc, AsyncNoneFunc, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(ValidValue));
    }

    /// <summary>
    /// Tests that the
    /// <see
    ///     cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, CancellationToken, Task{TResult}}, Func{CancellationToken, Task{TResult}}, CancellationToken)" />
    /// method asynchronously returns the correct value when the option does not contain a value (None) with a cancellation
    /// token.
    /// </summary>
    [Test]
    public async Task MatchAsync_WithCancellation_ShouldReturnNone_WhenOptionIsNone()
    {
        // Arrange
        var option = Option<string>.None;

        // Act
        var result = await option.MatchAsync(AsyncSomeFunc, AsyncNoneFunc, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(NoValueString));
    }

    [Test]
    public void Map_ShouldTransformValue_WhenSome()
    {
        var option = Option<string>.Some(ValidValue);

        var mapped = option.Map(value => value.Length);

        Assert.Multiple(() =>
        {
            Assert.That(mapped.HasValue, Is.True);
            Assert.That(mapped.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public void Map_ShouldReturnNone_WhenNone()
    {
        var option = Option<string>.None;

        var mapped = option.Map(value => value.Length);

        Assert.That(mapped.HasValue, Is.False);
    }

    [Test]
    public async Task MapAsync_ShouldTransformValue_WhenSome()
    {
        var option = Option<string>.Some(ValidValue);

        var mapped = await option.MapAsync(value => Task.FromResult(value.Length));

        Assert.Multiple(() =>
        {
            Assert.That(mapped.HasValue, Is.True);
            Assert.That(mapped.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public async Task MapAsync_ShouldTransformValue_WhenSome_WithCancellationToken()
    {
        var option = Option<string>.Some(ValidValue);

        var mapped = await option.MapAsync((value, _) => Task.FromResult(value.Length), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(mapped.HasValue, Is.True);
            Assert.That(mapped.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public async Task MapAsync_ShouldReturnNone_WhenNone()
    {
        var option = Option<string>.None;

        var mapped = await option.MapAsync(value => Task.FromResult(value.Length));

        Assert.That(mapped.HasValue, Is.False);
    }

    [Test]
    public async Task MapAsync_ShouldReturnNone_WhenNone_WithCancellationToken()
    {
        var option = Option<string>.None;

        var mapped = await option.MapAsync((value, _) => Task.FromResult(value.Length), CancellationToken.None);

        Assert.That(mapped.HasValue, Is.False);
    }

    [Test]
    public void Bind_ShouldTransformValue_WhenSome()
    {
        var option = Option<string>.Some(ValidValue);

        var bound = option.Bind(value => Option<int>.Some(value.Length));

        Assert.Multiple(() =>
        {
            Assert.That(bound.HasValue, Is.True);
            Assert.That(bound.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public void Bind_ShouldReturnNone_WhenNone()
    {
        var option = Option<string>.None;

        var bound = option.Bind(value => Option<int>.Some(value.Length));

        Assert.That(bound.HasValue, Is.False);
    }

    [Test]
    public async Task BindAsync_ShouldTransformValue_WhenSome()
    {
        var option = Option<string>.Some(ValidValue);

        var bound = await option.BindAsync(value => Task.FromResult(Option<int>.Some(value.Length)));

        Assert.Multiple(() =>
        {
            Assert.That(bound.HasValue, Is.True);
            Assert.That(bound.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public async Task BindAsync_ShouldTransformValue_WhenSome_WithCancellationToken()
    {
        var option = Option<string>.Some(ValidValue);

        var bound = await option.BindAsync((value, _) => Task.FromResult(Option<int>.Some(value.Length)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(bound.HasValue, Is.True);
            Assert.That(bound.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public async Task BindAsync_ShouldReturnNone_WhenNone()
    {
        var option = Option<string>.None;

        var bound = await option.BindAsync(value => Task.FromResult(Option<int>.Some(value.Length)));

        Assert.That(bound.HasValue, Is.False);
    }

    [Test]
    public async Task BindAsync_ShouldReturnNone_WhenNone_WithCancellationToken()
    {
        var option = Option<string>.None;

        var bound = await option.BindAsync((value, _) => Task.FromResult(Option<int>.Some(value.Length)),
            CancellationToken.None);

        Assert.That(bound.HasValue, Is.False);
    }

    [Test]
    public void Select_ShouldTransformValue_WhenSome()
    {
        var option = Option<string>.Some(ValidValue);

        var selected = option.Select(value => value.Length);

        Assert.Multiple(() =>
        {
            Assert.That(selected.HasValue, Is.True);
            Assert.That(selected.Value, Is.EqualTo(ValidValue.Length));
        });
    }

    [Test]
    public void SelectMany_ShouldCompose_WhenBothSome()
    {
        var first = Option<int>.Some(3);
        var second = Option<int>.Some(7);

        var query = from x in first
            from y in second
            select x + y;

        Assert.Multiple(() =>
        {
            Assert.That(query.HasValue, Is.True);
            Assert.That(query.Value, Is.EqualTo(10));
        });
    }

    [Test]
    public void SelectMany_ShouldReturnNone_WhenOuterIsNone()
    {
        var first = Option<int>.None;

        var query = from x in first
            from y in Option<int>.Some(7)
            select x + y;

        Assert.That(query.HasValue, Is.False);
    }

    [Test]
    public void SelectMany_ShouldReturnNone_WhenInnerIsNone()
    {
        var first = Option<int>.Some(3);

        var query = from x in first
            from y in Option<int>.None
            select x + y;

        Assert.That(query.HasValue, Is.False);
    }

    [Test]
    public void Map_ShouldThrow_WhenTransformIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.Throws<ArgumentNullException>(() => _ = option.Map<int>(null!));
    }

    [Test]
    public void Bind_ShouldThrow_WhenFunctionIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.Throws<ArgumentNullException>(() => _ = option.Bind<int>(null!));
    }

    [Test]
    public void Select_ShouldThrow_WhenSelectorIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.Throws<ArgumentNullException>(() => _ = option.Select<int>(null!));
    }

    [Test]
    public void SelectMany_ShouldThrow_WhenSelectorIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.Throws<ArgumentNullException>(() => _ = option.SelectMany<int, int>(null!, (_, _) => 0));
    }

    [Test]
    public void SelectMany_ShouldThrow_WhenResultSelectorIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.Throws<ArgumentNullException>(() =>
            _ = option.SelectMany<int, int>(value => Option<int>.Some(value.Length), null!));
    }

    [Test]
    public async Task MapAsync_ShouldThrow_WhenTransformIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await option.MapAsync<int>(null!));
    }

    [Test]
    public async Task MapAsync_WithCancellation_ShouldThrow_WhenTransformIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await option.MapAsync<int>(null!, CancellationToken.None));
    }

    [Test]
    public async Task BindAsync_ShouldThrow_WhenFunctionIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.ThrowsAsync<ArgumentNullException>(async () => await option.BindAsync<int>(null!));
    }

    [Test]
    public async Task BindAsync_WithCancellation_ShouldThrow_WhenFunctionIsNull()
    {
        var option = Option<string>.Some(ValidValue);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await option.BindAsync<int>(null!, CancellationToken.None));
    }

    [Test]
    public void Map_ShouldThrow_WhenOptionIsInvalid()
    {
        var invalidOption = new Mock<Option<string>>().Object;

        Assert.Throws<OptionIsNoneException>(() => _ = invalidOption.Map(value => value.Length));
    }

    [Test]
    public void Bind_ShouldThrow_WhenOptionIsInvalid()
    {
        var invalidOption = new Mock<Option<string>>().Object;

        Assert.Throws<OptionIsNoneException>(() => _ = invalidOption.Bind(value => Option<int>.Some(value.Length)));
    }

    [Test]
    public async Task MapAsync_ShouldThrow_WhenOptionIsInvalid()
    {
        var invalidOption = new Mock<Option<string>>().Object;

        Assert.ThrowsAsync<OptionIsNoneException>(async () =>
            await invalidOption.MapAsync(value => Task.FromResult(value.Length)));
    }

    [Test]
    public async Task BindAsync_ShouldThrow_WhenOptionIsInvalid()
    {
        var invalidOption = new Mock<Option<string>>().Object;

        Assert.ThrowsAsync<OptionIsNoneException>(async () =>
            await invalidOption.BindAsync(value => Task.FromResult(Option<int>.Some(value.Length))));
    }

    [Test]
    public async Task MapAsync_WithCancellation_ShouldThrow_WhenOptionIsInvalid()
    {
        var invalidOption = new Mock<Option<string>>().Object;

        Assert.ThrowsAsync<OptionIsNoneException>(async () =>
            await invalidOption.MapAsync((value, _) => Task.FromResult(value.Length), CancellationToken.None));
    }

    [Test]
    public async Task BindAsync_WithCancellation_ShouldThrow_WhenOptionIsInvalid()
    {
        var invalidOption = new Mock<Option<string>>().Object;

        Assert.ThrowsAsync<OptionIsNoneException>(async () =>
            await invalidOption.BindAsync((value, _) => Task.FromResult(Option<int>.Some(value.Length)),
                CancellationToken.None));
    }

    [Test]
    public void SelectMany_ShouldThrow_WhenIntermediateOptionIsInvalid()
    {
        var option = Option<string>.Some(ValidValue);
        var invalidIntermediate = new Mock<Option<int>>().Object;

        Assert.Throws<OptionIsNoneException>(() =>
            _ = option.SelectMany(_ => invalidIntermediate, (_, value) => value));
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.Match{TResult}(Func{TValue, TResult}, Func{TResult})" /> method throws an
    /// exception
    /// when the option is invalid.
    /// </summary>
    [Test]
    public void Match_ShouldThrow_WhenOptionIsInvalid()
    {
        // Arrange
        var mockOption = new Mock<Option<string>>();

        mockOption.Setup(m => m.HasValue).Returns(false);
        mockOption.Setup(m => m.Value).Throws(() => new OptionIsNoneException(typeof(string).Name));

        // Act & Assert
        Assert.Throws<OptionIsNoneException>(TestDelegate);
        return;

        void TestDelegate()
        {
            mockOption.Object.Match(SomeFunc, NoneFunc);
        }
    }

    /// <summary>
    /// Tests that the
    /// <see
    ///     cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, CancellationToken, Task{TResult}}, Func{CancellationToken, Task{TResult}}, CancellationToken)" />
    /// method asynchronously throws an exception when the option is invalid.
    /// </summary>
    [Test]
    public void MatchAsync_WithCancellation_ShouldThrow_WhenOptionIsInvalid()
    {
        // Arrange
        var mockOption = new Mock<Option<string>>();

        mockOption.Setup(m => m.HasValue).Returns(false);
        mockOption.Setup(m => m.Value).Throws(() => new OptionIsNoneException(typeof(string).Name));

        // Act & Assert
        Assert.ThrowsAsync<OptionIsNoneException>(AsyncTestDelegate);
        return;

        async Task AsyncTestDelegate()
        {
            await mockOption.Object.MatchAsync(AsyncSomeFunc, AsyncNoneFunc, CancellationToken.None);
        }
    }

    /// <summary>
    /// Tests that the
    /// <see
    ///     cref="Option{TValue}.MatchAsync{TResult}(Func{TValue, Task{TResult}}, Func{Task{TResult}})" />
    /// method asynchronously throws an exception when the option is invalid.
    /// </summary>
    [Test]
    public void MatchAsync_ShouldThrow_WhenOptionIsInvalid()
    {
        // Arrange
        var mockOption = new Mock<Option<string>>();

        mockOption.Setup(m => m.HasValue).Returns(false);
        mockOption.Setup(m => m.Value).Throws(() => new OptionIsNoneException(typeof(string).Name));

        // Act & Assert
        Assert.ThrowsAsync<OptionIsNoneException>(AsyncTestDelegate);
        return;

        async Task AsyncTestDelegate()
        {
            await mockOption.Object.MatchAsync(AsyncSomeFunc, AsyncNoneFunc);
        }
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.FromNullable" /> method returns a <see cref="Some{TValue}" /> when the value
    /// is not
    /// null.
    /// </summary>
    [Test]
    public void FromNullable_ShouldReturnSome_WhenValueIsNotNull()
    {
        // Act
        var option = Option<string>.FromNullable(ValidValue);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(option.HasValue, Is.True);
            Assert.That(option.Value, Is.EqualTo(ValidValue));
        });
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.FromNullable" /> method returns a <see cref="None{TValue}" /> when the value
    /// is
    /// null.
    /// </summary>
    [Test]
    public void FromNullable_ShouldReturnNone_WhenValueIsNull()
    {
        // Act
        var option = Option<string>.FromNullable(NullValue);

        // Assert
        Assert.That(option.HasValue, Is.False);
        Assert.Throws<OptionIsNoneException>(() => _ = option.Value);
    }

    /// <summary>
    /// Tests that implicit conversion from a non-nullable value to an <see cref="Option{TValue}" /> creates a
    /// <see cref="Some{TValue}" />.
    /// </summary>
    [Test]
    public void ImplicitConversion_ShouldReturnSome_WhenValueIsNotNull()
    {
        // Act
        Option<string> option = ValidValue;

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(option.HasValue, Is.True);
            Assert.That(option.Value, Is.EqualTo(ValidValue));
        });
    }

    /// <summary>
    /// Tests that implicit conversion from a null value to an <see cref="Option{TValue}" /> creates a
    /// <see cref="None{TValue}" />.
    /// </summary>
    [Test]
    public void ImplicitConversion_ShouldReturnNone_WhenValueIsNull()
    {
        // Act
        Option<string> option = NullValue;

        // Assert
        Assert.That(option.HasValue, Is.False);
        Assert.Throws<OptionIsNoneException>(() => _ = option.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.ToString" /> method returns a correct string representation when the option
    /// contains
    /// a
    /// value (Some).
    /// </summary>
    [Test]
    public void ToString_ShouldReturnSome_WhenValueIsNotNull()
    {
        // Act
        var option = Option<string>.FromNullable(ValidValue);
        var stringValue = option.ToString();

        // Assert
        Assert.That(stringValue, Is.EqualTo($"Some({ValidValue})"));
    }

    /// <summary>
    /// Tests that the <see cref="Option{TValue}.ToString" /> method returns a correct string representation when the option
    /// does
    /// not
    /// contain a value (None).
    /// </summary>
    [Test]
    public void ToString_ShouldReturnNone_WhenValueIsNotNull()
    {
        // Act
        var option = Option<string>.FromNullable(NullValue);
        var stringValue = option.ToString();

        // Assert
        Assert.That(stringValue, Is.EqualTo("None"));
    }

    /// <summary>
    /// A helper method that returns the input value as a string when the option contains a value (Some).
    /// </summary>
    /// <param name="value">The input string value.</param>
    /// <returns>The same input string value.</returns>
    private static string SomeFunc(string value)
    {
        return value;
    }

    /// <summary>
    /// A helper method that returns a predefined string when the option does not contain a value (None).
    /// </summary>
    /// <returns>A string indicating "No Value".</returns>
    private static string NoneFunc()
    {
        return NoValueString;
    }

    /// <summary>
    /// An asynchronous helper method that returns a predefined string asynchronously when the option does not contain a value
    /// (None).
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, with a result of "No Value".</returns>
    private static async Task<string> AsyncNoneFunc(CancellationToken ct)
    {
        return await Task.FromResult(NoValueString);
    }

    /// <summary>
    /// An asynchronous helper method that returns the input value as a string asynchronously when the option contains a value
    /// (Some).
    /// </summary>
    /// <param name="value">The input string value.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, with a result of the input value.</returns>
    private static async Task<string> AsyncSomeFunc(string value, CancellationToken ct)
    {
        return await Task.FromResult(value);
    }

    /// <summary>
    /// An asynchronous helper method that returns a predefined string asynchronously when the option does not contain a value
    /// (None).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, with a result of "No Value".</returns>
    private static async Task<string> AsyncNoneFunc()
    {
        return await Task.FromResult(NoValueString);
    }

    /// <summary>
    /// An asynchronous helper method that returns the input value as a string asynchronously when the option contains a value
    /// (Some).
    /// </summary>
    /// <param name="value">The input string value.</param>
    /// <returns>A task that represents the asynchronous operation, with a result of the input value.</returns>
    private static async Task<string> AsyncSomeFunc(string value)
    {
        return await Task.FromResult(value);
    }
}
