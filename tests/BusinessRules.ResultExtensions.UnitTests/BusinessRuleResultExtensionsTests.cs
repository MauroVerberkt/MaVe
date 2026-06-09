using MaVe.Monads;

namespace MaVe.BusinessRules.ResultExtensions.UnitTests;

[TestFixture]
public class BusinessRuleResultExtensionsTests
{
    [Test]
    public void ToResult_WithBusinessRuleViolationException_ReturnsFailedResult()
    {
        // Arrange
        var exception = TestUserMustBeAdult.ToException();

        // Act
        var result = exception.ToResult<string>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error?.Exception, Is.SameAs(exception));
        });
    }

    [Test]
    public void ToResult_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        BusinessRuleViolationException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = exception!.ToResult<string>());
    }

    [Test]
    public void ToResult_FromBusinessRule_ReturnsFailedResultWithBusinessRuleViolationException()
    {
        // Arrange
        var rule = new TestUserMustBeAdult();
        var innerError = new InvalidOperationException("Test error");

        // Act
        var result = rule.ToResult<TestUserMustBeAdult, int>(innerError);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.Multiple(() =>
        {
            Assert.That(brException.Key, Is.EqualTo("TEST_USER_AGE_MIN"));
            Assert.That(brException.InnerException, Is.SameAs(innerError));
        });
    }

    [Test]
    public void ValidateAndReturn_WithSuccessfulOperation_ReturnsSuccessResult()
    {
        // Arrange
        const int expectedValue = 42;

        // Act
        var result = BusinessRuleResultExtensions.ValidateAndReturn(() => expectedValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public void ValidateAndReturn_WithBusinessRuleViolationException_ReturnsFailedResult()
    {
        // Arrange & Act
        var result = BusinessRuleResultExtensions.ValidateAndReturn<int>(() => throw TestUserMustBeAdult.ToException());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });
    }

    [Test]
    public void ValidateAndReturn_WithRule_WithSuccessfulOperation_ReturnsSuccessResult()
    {
        // Arrange
        const string expectedValue = "test";
        var rule = new TestUserMustBeAdult();

        // Act
        var result = BusinessRuleResultExtensions.ValidateAndReturn(() => expectedValue, rule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public void ValidateAndReturn_WithRule_WithBusinessRuleViolationException_ReturnsFailedResult()
    {
        // Arrange
        var rule = new TestUserMustBeAdult();

        // Act
        var result = BusinessRuleResultExtensions.ValidateAndReturn<int>(
            () => throw TestUserMustBeAdult.ToException(), rule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });
    }

    [Test]
    public void ValidateAndReturn_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        Func<int>? operation = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = BusinessRuleResultExtensions.ValidateAndReturn(operation!));
    }

    [Test]
    public async Task ValidateAndReturnAsync_WithSuccessfulOperation_ReturnsSuccessResult()
    {
        // Arrange
        const string expectedValue = "async result";

        // Act
        var result = await BusinessRuleResultExtensions.ValidateAndReturnAsync(async () =>
        {
            await Task.Delay(1);
            return expectedValue;
        });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public async Task ValidateAndReturnAsync_WithBusinessRuleViolationException_ReturnsFailedResult()
    {
        // Arrange & Act
        var result = await BusinessRuleResultExtensions.ValidateAndReturnAsync<string>(async () =>
        {
            await Task.Delay(1);
            throw TestPasswordMinLength.ToException();
        });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.That(brException.Key, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
    }

    [Test]
    public async Task ValidateAndReturnAsync_WithRule_WithSuccessfulOperation_ReturnsSuccessResult()
    {
        // Arrange
        const int expectedValue = 100;
        var rule = new TestUserMustBeAuthenticated();

        // Act
        var result = await BusinessRuleResultExtensions.ValidateAndReturnAsync(
            async () =>
            {
                await Task.Delay(1);
                return expectedValue;
            },
            rule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public async Task ValidateAndReturnAsync_WithRule_WithBusinessRuleViolationException_ReturnsFailedResult()
    {
        // Arrange
        var rule = new TestUserMustBeAdult();

        // Act
        var result = await BusinessRuleResultExtensions.ValidateAndReturnAsync<string>(
            async () =>
            {
                await Task.Delay(1);
                throw TestUserMustBeAdult.ToException();
            }, rule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });
    }

    [Test]
    public void EnsureBusinessRule_WithPassingPredicate_ReturnsSuccessResult()
    {
        // Arrange
        const int age = 25;
        var rule = new TestUserMustBeAdult();

        // Act
        var result = age.EnsureBusinessRule(a => a >= 18, rule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(age));
        });
    }

    [Test]
    public void EnsureBusinessRule_WithFailingPredicate_ReturnsFailedResult()
    {
        // Arrange
        const int age = 16;
        var rule = new TestUserMustBeAdult();

        // Act
        var result = age.EnsureBusinessRule(a => a >= 18, rule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.Multiple(() =>
        {
            Assert.That(brException.Key, Is.EqualTo("TEST_USER_AGE_MIN"));
            Assert.That(brException.Requirement, Is.EqualTo("User must be at least 18 years old"));
        });
    }

    [Test]
    public void EnsureBusinessRule_WithCustomMessage_WithFailingPredicate_ReturnsFailedResultWithCustomMessage()
    {
        // Arrange
        const string password = "short";
        var rule = new TestPasswordMinLength();
        var customMessage = $"Password has only {password.Length} characters";

        // Act
        var result = password.EnsureBusinessRule(
            p => p.Length >= 8,
            rule,
            customMessage);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.Multiple(() =>
        {
            Assert.That(brException.Message, Is.EqualTo(customMessage));
            Assert.That(brException.Key, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
        });
    }

    [Test]
    public void EnsureBusinessRule_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        const string? value = null;
        var rule = new TestEmailMustBeValid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = value!.EnsureBusinessRule(_ => true, rule));
    }

    [Test]
    public void EnsureBusinessRule_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        const string value = "test";
        var rule = new TestEmailMustBeValid();
        Func<string, bool>? predicate = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = value.EnsureBusinessRule(predicate!, rule));
    }

    [Test]
    public void EnsureBusinessRule_WithNullRule_ThrowsArgumentNullException()
    {
        // Arrange
        const string value = "test";
        BusinessRuleBase? rule = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = value.EnsureBusinessRule(_ => true, rule!));
    }

    [Test]
    public void ValidateAll_WithAllPassingPredicates_ReturnsSuccessResult()
    {
        // Arrange
        const string password = "SecurePass123";
        var minLengthRule = new TestPasswordMinLength();
        var uppercaseRule = new TestPasswordMustContainUppercase();
        var numberRule = new TestPasswordMustContainNumber();

        // Act
        var result = password.ValidateAll(
            (p => p.Length >= 8, minLengthRule),
            (p => p.Any(char.IsUpper), uppercaseRule),
            (p => p.Any(char.IsDigit), numberRule)
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo(password));
        });
    }

    [Test]
    public void ValidateAll_WithFirstFailingPredicate_ReturnsFailedResultWithFirstRule()
    {
        // Arrange
        const string password = "short";
        var minLengthRule = new TestPasswordMinLength();
        var uppercaseRule = new TestPasswordMustContainUppercase();
        var numberRule = new TestPasswordMustContainNumber();

        // Act
        var result = password.ValidateAll(
            (p => p.Length >= 8, minLengthRule),
            (p => p.Any(char.IsUpper), uppercaseRule),
            (p => p.Any(char.IsDigit), numberRule)
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.That(brException.Key, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
    }

    [Test]
    public void ValidateAll_WithSecondFailingPredicate_ReturnsFailedResultWithSecondRule()
    {
        // Arrange
        const string password = "nouppercase123";
        var minLengthRule = new TestPasswordMinLength();
        var uppercaseRule = new TestPasswordMustContainUppercase();
        var numberRule = new TestPasswordMustContainNumber();

        // Act
        var result = password.ValidateAll(
            (p => p.Length >= 8, minLengthRule),
            (p => p.Any(char.IsUpper), uppercaseRule),
            (p => p.Any(char.IsDigit), numberRule)
        );

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.That(brException.Key, Is.EqualTo("TEST_PWD_UPPERCASE"));
    }

    [Test]
    public void ValidateAll_WithCustomMessages_WithFailingPredicate_ReturnsFailedResultWithCustomMessage()
    {
        // Arrange
        const string password = "short";
        var minLengthRule = new TestPasswordMinLength();
        var uppercaseRule = new TestPasswordMustContainUppercase();
        const string customMessage = "Password is too short!";

        // Act
        var result = password.ValidateAll(
            (p => p.Length >= 8, minLengthRule, customMessage),
            (p => p.Any(char.IsUpper), uppercaseRule, "Missing uppercase")
        );

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var brException = (BusinessRuleViolationException)result.Error!.Exception!;
        Assert.Multiple(() =>
        {
            Assert.That(brException.Message, Is.EqualTo(customMessage));
            Assert.That(brException.Key, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
        });
    }

    [Test]
    public void ValidateAll_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        const string? value = null;
        var rule = new TestPasswordMinLength();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = value!.ValidateAll((_ => true, rule)));
    }

    [Test]
    public void ValidateAll_WithNullValidationsArray_ThrowsArgumentNullException()
    {
        // Arrange
        const string value = "test";
        (Func<string, bool> predicate, BusinessRuleBase rule)[]? validations = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = value.ValidateAll(validations!));
    }

    [Test]
    public void ValidateAll_WithNullPredicateInArray_ThrowsArgumentException()
    {
        // Arrange
        const string value = "test";
        var rule = new TestPasswordMinLength();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _ = value.ValidateAll((null!, rule)));
    }

    [Test]
    public void ValidateAll_WithNullRuleInArray_ThrowsArgumentException()
    {
        // Arrange
        const string value = "test";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _ = value.ValidateAll((_ => true, null!)));
    }

    [Test]
    public void ValidateAll_WithCustomMessages_WithNullOrWhitespaceErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        const string value = "test";
        var rule = new TestPasswordMinLength();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _ = value.ValidateAll((_ => true, rule, "")));
    }

    [Test]
    public void ToBusinessRuleException_WithFailedResult_ReturnsBusinessRuleViolationException()
    {
        // Arrange
        var originalError = new InvalidOperationException("Original error");
        var result = Result.Failure<string>(Error.Unexpected(originalError));
        var rule = new TestUserMustBeAdult();

        // Act
        var exception = result.ToBusinessRuleException(rule);

        // Assert
        Assert.That(exception, Is.InstanceOf<BusinessRuleViolationException>());
        Assert.Multiple(() =>
        {
            Assert.That(exception.Key, Is.EqualTo("TEST_USER_AGE_MIN"));
            Assert.That(exception.InnerException, Is.SameAs(originalError));
            Assert.That(exception.Message, Is.EqualTo(originalError.Message));
        });
    }

    [Test]
    public void ToBusinessRuleException_WithSuccessfulResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result.Success("test value");
        var rule = new TestUserMustBeAdult();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _ = result.ToBusinessRuleException(rule));

        Assert.That(ex.Message, Is.EqualTo("Cannot convert successful result to exception"));
    }

    [Test]
    public void ToBusinessRuleException_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        Result<string>? result = null;
        var rule = new TestUserMustBeAdult();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = result!.ToBusinessRuleException(rule));
    }

    [Test]
    public void ToBusinessRuleException_WithNullRule_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result.Failure<string>(Error.Create("test"));
        BusinessRuleBase? rule = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _ = result.ToBusinessRuleException(rule!));
    }

    [Test]
    public void ToBusinessRuleException_WithBusinessRuleViolationError_ReturnsOriginalException()
    {
        // Arrange
        var originalException = TestUserMustBeAdult.ToException();
        var result = Result.Failure<string>(BusinessRuleResultExtensions.FromViolation(originalException));
        var rule = new TestUserMustBeAdult();

        // Act
        var returnedException = result.ToBusinessRuleException(rule);

        // Assert
        Assert.That(returnedException, Is.SameAs(originalException));
    }

    [Test]
    public void ToBusinessRuleException_WithErrorWithoutException_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result.Failure<string>(Error.Create("some message"));
        var rule = new TestUserMustBeAdult();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _ = result.ToBusinessRuleException(rule));

        Assert.That(ex.Message, Does.Contain("no inner exception present"));
    }
}
