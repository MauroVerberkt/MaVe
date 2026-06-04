using HelperMonads;

namespace BusinessRules.ResultExtensions.UnitTests;

/// <summary>
/// Composition tests demonstrating multi-step validation scenarios
/// </summary>
[TestFixture]
public class CompositionTests
{
    private class User
    {
        public string Username { get; init; } = string.Empty;
        public int Age { get; init; }
        public string Password { get; init; } = string.Empty;
    }

    [Test]
    public void UserCreation_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        const string username = "john_doe";
        const int age = 25;
        const string password = "SecurePass123";

        // Act
        var result = CreateUser(username, age, password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Username, Is.EqualTo(username));
            Assert.That(result.Data.Age, Is.EqualTo(age));
            Assert.That(result.Data.Password, Is.EqualTo(password));
        });
    }

    [Test]
    public void UserCreation_WithInvalidAge_ReturnsFailureResult()
    {
        // Arrange
        const string username = "jane_doe";
        const int age = 16;
        const string password = "SecurePass123";

        // Act
        var result = CreateUser(username, age, password);

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Key, Is.EqualTo("TEST_USER_AGE_MIN"));
    }

    [Test]
    public void UserCreation_WithShortPassword_ReturnsFailureResult()
    {
        // Arrange
        const string username = "bob";
        const int age = 30;
        const string password = "short";

        // Act
        var result = CreateUser(username, age, password);

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Key, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
    }

    [Test]
    public void UserCreation_WithPasswordMissingUppercase_ReturnsFailureResult()
    {
        // Arrange
        const string username = "alice";
        const int age = 28;
        const string password = "password123";

        // Act
        var result = CreateUser(username, age, password);

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Key, Is.EqualTo("TEST_PWD_UPPERCASE"));
    }

    [Test]
    public void UserCreation_WithPasswordMissingNumber_ReturnsFailureResult()
    {
        // Arrange
        const string username = "charlie";
        const int age = 22;
        const string password = "SecurePassword";

        // Act
        var result = CreateUser(username, age, password);

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Key, Is.EqualTo("TEST_PWD_NUMBER"));
    }

    [Test]
    public async Task AsyncUserCreation_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        const string username = "async_user";
        const int age = 30;
        const string password = "SecurePass123";

        // Act
        var result = await CreateUserAsync(username, age, password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Username, Is.EqualTo(username));
        });
    }

    [Test]
    public async Task AsyncUserCreation_WithInvalidAge_ReturnsFailureResult()
    {
        // Arrange
        const string username = "young_user";
        const int age = 15;
        const string password = "SecurePass123";

        // Act
        var result = await CreateUserAsync(username, age, password);

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception!.Key, Is.EqualTo("TEST_USER_AGE_MIN"));
    }

    [Test]
    public void ChainedValidation_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        const int age = 25;
        const string password = "SecurePass123";

        // Act
        var result = ValidateAge(age)
            .BindAndTransform(_ => ValidatePassword(password))
            .Map(validPassword => new User { Age = age, Password = validPassword });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Age, Is.EqualTo(age));
        });
    }

    [Test]
    public void ChainedValidation_WithInvalidAge_ShortCircuitsAtFirstFailure()
    {
        // Arrange
        const int age = 16;
        const string password = "SecurePass123";

        // Act
        var result = ValidateAge(age)
            .BindAndTransform(_ => ValidatePassword(password))
            .Map(validPassword => new User { Age = age, Password = validPassword });

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception!.Key, Is.EqualTo("TEST_USER_AGE_MIN"));
    }

    [Test]
    public void ChainedValidation_WithValidAgeButInvalidPassword_FailsAtPasswordValidation()
    {
        // Arrange
        const int age = 25;
        const string password = "short";

        // Act
        var result = ValidateAge(age)
            .BindAndTransform(_ => ValidatePassword(password))
            .Map(validPassword => new User { Age = age, Password = validPassword });

        // Assert
        Assert.That(result.IsFailure, Is.True);

        var exception = result.Error?.Exception as BusinessRuleViolationException;
        Assert.That(exception!.Key, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
    }

    [Test]
    public void OnSuccess_WithSuccessfulResult_ExecutesAction()
    {
        // Arrange
        var actionExecuted = false;
        const int age = 25;

        // Act
        var result = ValidateAge(age)
            .OnSuccess(_ => actionExecuted = true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(actionExecuted, Is.True);
        });
    }

    [Test]
    public void OnFailure_WithFailedResult_ExecutesAction()
    {
        // Arrange
        Error? capturedError = null;
        const int age = 16;

        // Act
        var result = ValidateAge(age)
            .OnFailure(error => capturedError = error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(capturedError, Is.Not.Null);
            Assert.That(capturedError!.Exception, Is.InstanceOf<BusinessRuleViolationException>());
        });
    }

    // Helper methods simulating real-world service methods

    private Result<User> CreateUser(string username, int age, string password)
    {
        return ValidateAge(age)
            .BindAndTransform(_ => ValidatePassword(password))
            .Map(validPassword => new User { Username = username, Age = age, Password = validPassword });
    }

    private async Task<Result<User>> CreateUserAsync(string username, int age, string password)
    {
        var ageResult = await ValidateAgeAsync(age);
        var passwordResult =
            await ageResult.BindAndTransformAsync(async _ => await ValidatePasswordAsync(password));

        return await passwordResult.MapAsync(async validPassword =>
        {
            await Task.Delay(1); // Simulate async operation
            return new User { Username = username, Age = age, Password = validPassword };
        });
    }

    private Result<int> ValidateAge(int age)
    {
        return age.EnsureBusinessRule(
            a => a >= 18,
            new TestUserMustBeAdult(),
            $"Age {age} is below minimum requirement of 18");
    }

    private async Task<Result<int>> ValidateAgeAsync(int age)
    {
        await Task.Delay(1); // Simulate async operation
        return age.EnsureBusinessRule(
            a => a >= 18,
            new TestUserMustBeAdult(),
            $"Age {age} is below minimum requirement of 18");
    }

    private Result<string> ValidatePassword(string password)
    {
        return password.ValidateAll(
            (p => p.Length >= 8, new TestPasswordMinLength(), "Password is too short"),
            (p => p.Any(char.IsUpper), new TestPasswordMustContainUppercase(), "Password must have uppercase letters"),
            (p => p.Any(char.IsDigit), new TestPasswordMustContainNumber(), "Password must contain numbers")
        );
    }

    private async Task<Result<string>> ValidatePasswordAsync(string password)
    {
        await Task.Delay(1); // Simulate async operation
        return password.ValidateAll(
            (p => p.Length >= 8, new TestPasswordMinLength(), "Password is too short"),
            (p => p.Any(char.IsUpper), new TestPasswordMustContainUppercase(), "Password must have uppercase letters"),
            (p => p.Any(char.IsDigit), new TestPasswordMustContainNumber(), "Password must contain numbers")
        );
    }
}
