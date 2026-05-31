---
sidebar_position: 2
title: Quick Start
description: Get up and running with Result types and Business Rules in 5 minutes
keywords:
  - quick start
  - tutorial
  - result
  - business rules
  - getting started
---

# Quick Start

This guide shows you the core patterns in under 5 minutes.

## Result Pattern

Use `Result<T>` to represent operations that can succeed or fail without throwing exceptions:

```csharp title="UserService.cs"
using HelperMonads;

public Result<User> GetUser(int id)
{
    var user = _repository.Find(id);
    if (user == null)
        return Result.Failure<User>(Error.Create($"User {id} not found", "NOT_FOUND"));

    return Result.Success(user);
}
```

Chain operations together using `Map` and `Bind`:

```csharp title="Chaining.cs"
var result = GetUser(42)
    .Map(user => user.Email)
    .OnSuccess(email => SendNotification(email))
    .OnFailure(error => _logger.LogError("Failed: {Error}", error.Message));
```

## Business Rules

Define rules in JSON, get strongly-typed classes at compile time:

```json title="MyApp.BusinessRules.json"
{
  "businessRules": [
    {
      "className": "UserMustBeAdult",
      "key": "USER_AGE_MIN",
      "requirement": "User must be at least 18 years old",
      "category": "UserValidation"
    }
  ]
}
```

Use them in code with full type safety:

```csharp title="Validation.cs"
using BusinessRules.Rules.UserValidation;

[ImplementsBusinessRule(UserMustBeAdult.Key)]
public void ValidateAge(int age)
{
    if (age < 18)
        throw UserMustBeAdult.ToException();
}
```

## Combining Both

Use `BusinessRules.ResultExtensions` for functional validation without exceptions:

```csharp title="FunctionalValidation.cs"
using BusinessRules.ResultExtensions;

public Result<User> CreateUser(string username, int age)
{
    return age
        .EnsureBusinessRule(a => a >= 18, UserMustBeAdult)
        .Map(validAge => new User { Username = username, Age = validAge });
}
```

## Union Types

Model domain alternatives with source-generated discriminated unions:

```csharp title="BusinessParty.cs"
using HelperUnions;

[Union]
public partial record BusinessParty
{
    public sealed record Customer(CustomerInfo Info) : BusinessParty;
    public sealed record Supplier(SupplierInfo Info) : BusinessParty;
    public sealed record Prospect() : BusinessParty;
}
```

Handle all variants exhaustively — missing one is a compile error:

```csharp title="Handler.cs"
var name = party
    .Match()
    .Customer(info => info.Name)
    .Supplier(info => info.CompanyName)
    .Prospect("Unknown")
    .Result();
```

:::info[Next Steps]

- Learn more about the [Result monad](../monads/result/index.md)
- Explore [Business Rules](../business-rules/overview.md) in depth
- Model domain alternatives with [Unions](../unions/overview.md)

:::
