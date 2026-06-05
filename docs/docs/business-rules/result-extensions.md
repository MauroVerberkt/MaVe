---
sidebar_position: 2
title: Result Extensions
description: Bridge between BusinessRules and the Result monad for functional validation
keywords:
  - result extensions
  - functional validation
  - railway oriented programming
  - ensure business rule
  - validate all
  - composable validation
---

# BusinessRules.ResultExtensions

**BusinessRules.ResultExtensions** bridges the BusinessRules framework with the Result monad from Monads. It provides extension methods that enable functional error handling with business rules - composable, declarative validation without exception-based control flow.

## Installation

```xml title="Project.csproj"
<ItemGroup>
  <ProjectReference Include="..\BusinessRules\BusinessRules.csproj" />
  <ProjectReference Include="..\Monads\Monads.csproj" />
  <ProjectReference Include="..\BusinessRules.ResultExtensions\BusinessRules.ResultExtensions.csproj" />
</ItemGroup>
```

## Core Extension Methods

### EnsureBusinessRule&lt;T&gt;

Validates a value against a predicate and returns a `Result<T>`:

```csharp title="EnsureBusinessRule.cs"
// Basic usage
Result<int> result = age.EnsureBusinessRule(
    a => a >= 18,
    UserMustBeAdult);

// With custom error message
Result<string> result = password.EnsureBusinessRule(
    p => p.Length >= 8,
    PasswordMinLength,
    $"Password has only {password.Length} characters");

// Chaining with Map
Result<User> userResult = age
    .EnsureBusinessRule(a => a >= 18, UserMustBeAdult)
    .Map(validAge => new User { Age = validAge });
```

### ValidateAll&lt;T&gt;

Validates a value against multiple rules, short-circuiting on first failure:

```csharp title="ValidateAll.cs"
Result<string> result = password.ValidateAll(
    (p => p.Length >= 8, PasswordMinLength),
    (p => p.Any(char.IsUpper), PasswordMustContainUppercase),
    (p => p.Any(char.IsDigit), PasswordMustContainNumber)
);

// With custom error messages
Result<User> result = user.ValidateAll(
    (u => u.Age >= 18, UserMustBeAdult, $"User is only {user.Age} years old"),
    (u => !string.IsNullOrEmpty(u.Email), UserMustHaveEmail, "Email is required"),
    (u => u.IsActive, UserMustBeActive, "User account is inactive")
);
```

### ValidateAndReturn&lt;T&gt;

Executes an operation and wraps the result, catching `BusinessRuleViolationException`:

```csharp title="ValidateAndReturn.cs"
Result<User> result = BusinessRuleResultExtensions.ValidateAndReturn(() =>
{
    if (age < 18)
        throw UserMustBeAdult.ToException();

    return new User { Age = age };
});
```

### ValidateAndReturnAsync&lt;T&gt;

Async version of `ValidateAndReturn`:

```csharp title="ValidateAndReturnAsync.cs"
Result<User> result = await BusinessRuleResultExtensions.ValidateAndReturnAsync(async () =>
{
    var user = await LoadUserAsync(userId);
    if (!user.IsActive)
        throw UserMustBeActive.ToException();

    return user;
});
```

### ToResult&lt;T&gt;

Converts a `BusinessRuleViolationException` to a failed `Result<T>`:

```csharp title="ToResult.cs"
try
{
    throw UserMustBeAdult.ToException();
}
catch (BusinessRuleViolationException ex)
{
    Result<User> result = ex.ToResult<User>();
    // result.IsFailure == true
}
```

### ToBusinessRuleException&lt;T&gt;

Converts a failed `Result<T>` back to a `BusinessRuleViolationException`:

```csharp title="ToBusinessRuleException.cs"
Result<User> result = ValidateUser(user);

if (result.IsFailure)
{
    throw result.ToBusinessRuleException(UserMustBeValid);
}
```

## Complete Examples

### Validation Chain

```csharp title="UserService.cs"
public class UserService
{
    public Result<User> CreateUser(string username, int age, string password)
    {
        return ValidateAge(age)
            .BindAndTransform(validAge => ValidatePassword(password))
            .Map(validPassword => new User
            {
                Username = username,
                Age = age,
                Password = validPassword
            });
    }

    private Result<int> ValidateAge(int age)
    {
        return age.EnsureBusinessRule(
            a => a >= 18,
            UserMustBeAdult,
            $"Age {age} is below minimum requirement of 18");
    }

    private Result<string> ValidatePassword(string password)
    {
        return password.ValidateAll(
            (p => p.Length >= 8, PasswordMinLength, "Password is too short"),
            (p => p.Any(char.IsUpper), PasswordMustContainUppercase, "Must have uppercase"),
            (p => p.Any(char.IsDigit), PasswordMustContainNumber, "Must contain numbers")
        );
    }
}
```

### Async Pipeline

```csharp title="AsyncPipeline.cs"
public async Task<Result<User>> RegisterUserAsync(UserDto dto)
{
    var validationResult = await ValidateUserDataAsync(dto);

    return await validationResult
        .BindAndTransformAsync(async validDto =>
            await CreateUserInDatabaseAsync(validDto))
        .MapAsync(async user =>
            await SendWelcomeEmailAsync(user));
}
```

### Web API Controller

```csharp title="UsersController.cs"
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(
            request.Username, request.Age, request.Password);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new
            {
                Error = result.Error.Message,
                RuleKey = (result.Error.Exception as BusinessRuleViolationException)?.Key,
                RuleDescription = (result.Error.Exception as BusinessRuleViolationException)?.Requirement
            });
    }
}
```

## Exception-Based vs Result-Based

| Exception-Based | Result-Based |
|-----------------|--------------|
| `try/catch` blocks | Explicit `Result<T>` return types |
| Hidden control flow | Visible in method signatures |
| Performance cost of throwing | No exception overhead |
| Harder to compose | Chain with `Map`, `Bind` |

```csharp title="Comparison.cs"
// Exception-based
public User CreateUser(string username, int age)
{
    if (age < 18)
        throw UserMustBeAdult.ToException();
    return new User { Username = username, Age = age };
}

// Result-based
public Result<User> CreateUser(string username, int age)
{
    return age
        .EnsureBusinessRule(a => a >= 18, UserMustBeAdult)
        .Map(validAge => new User { Username = username, Age = validAge })
        .OnFailure(error => LogError(error));
}
```

:::tip[Best Practices]

1. Use `ValidateAndReturn` to wrap legacy code that throws exceptions
2. Use `EnsureBusinessRule` for single predicate validations
3. Use `ValidateAll` when validating multiple rules on the same value
4. Keep predicates pure (no side effects)
5. Provide custom error messages for better user feedback

:::

## See Also

- [BusinessRules Overview](./overview.md) - Core framework documentation
- [Result Monad](../monads/result/index.md) - Result type documentation
