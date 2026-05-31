[![CI](https://github.com/MauroVerberkt/DotnetHelpers/actions/workflows/ci.yml/badge.svg)](https://github.com/MauroVerberkt/DotnetHelpers/actions/workflows/ci.yml)
[![codecov](https://codecov.io/github/MauroVerberkt/DotnetHelpers/graph/badge.svg)](https://app.codecov.io/github/MauroVerberkt/DotnetHelpers)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com)

# DotnetHelpers

Functional building blocks for .NET: explicit error handling with **Result\<T\>**, null-safe optionals with **Option\<T\>**, compile-time validated **Business Rules** powered by source generators and Roslyn analyzers, and source-generated **discriminated unions** with exhaustive matching.

No runtime reflection. No exceptions for control flow. Strong typing all the way down.

## Packages

| Package | Description |
|---------|-------------|
| **HelperMonads** | `Result<T>` and `Option<T>` monadic types — Map, Bind, Match with full async and CancellationToken support |
| **BusinessRulesManagement** | Define business rules in JSON, get strongly-typed classes at compile time via source generation |
| **HelperUnions** | Source-generated discriminated unions with exhaustive Match/Switch builders and Roslyn analyzer support |
| **BusinessRules.ResultExtensions** | Bridge between BusinessRules validation and the Result pattern |
| **BusinessRules.Wcf** | WCF `FaultException` support for business rule violations |

## Getting Started

```bash
dotnet add package HelperMonads
```

```csharp
using HelperMonads;

// Explicit success/failure — no exceptions for expected error paths
public Result<User> GetUser(int id)
{
    var user = _repository.Find(id);
    return user is not null
        ? Result.Success(user)
        : Result.Failure<User>(new UserNotFoundException(id));
}

// Chain operations — failures short-circuit automatically
var email = GetUser(42)
    .Map(user => user.Email)
    .OnSuccess(addr => _logger.LogInformation("Found: {Email}", addr))
    .OnFailure(error => _logger.LogWarning(error, "User not found"));
```

## Status

> **Pre-release** — All packages are at `0.x`. APIs are stabilizing but may still change.

See [active proposals](docs/architecture/proposals/active/) for planned work.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for setup instructions, branch naming
conventions, commit message format, and CI workflow details.

## Documentation

📖 **[Read the docs](https://mauroverberkt.github.io/DotnetHelpers/)**

To run locally:

```bash
cd docs
npm install
npm start
```
