# Business Rules Analyzer

A Roslyn analyzer that enforces business rule validation patterns in your C# code.

## Features

### BR001: Business Rule Key Validation

Ensures that all business rule keys referenced in `BusinessRule` and `ImplementsBusinessRule` attributes actually exist
in a `BusinessRule` field.

### BR002/BR003: Validation Coverage

Ensures that every `BusinessRule` has a corresponding `ImplementsBusinessRule` with the same `ruleKey` **anywhere in the
compilation**.

- **BR002** (Error): When `enforceValidation=true` (default)
- **BR003** (Warning): When `enforceValidation=false`

## How It Works

The analyzer uses a **compilation-wide matching approach**:

1. **Index Phase**: Scans all methods and classes in the compilation and builds an index of all `ruleKey` values found
   in `ImplementsBusinessRule` attributes
2. **Validation Phase**: For each `BusinessRule`, checks if its `ruleKey` exists in the index
3. **Report Phase**: Reports diagnostics for any unvalidated requirements

This means:

- ✅ Validators can be in **any class or method** in your project
- ✅ Validators can be **reused** across multiple requirement sites
- ✅ Validators don't need to be in the same class as the requirement
- ⚠️ Validators must be in the **same compilation** (not in referenced assemblies)

## Usage Examples

### Valid: Validator in Same Class

```csharp
public class MyService
{
    [ImplementsBusinessRule("USER_AUTH")]
    public void ValidateAuth() { }
    
    [BusinessRule("USER_AUTH")]
    public void DoSomething() { }
}
```

### Valid: Validator in Different Class

```csharp
public class Validators
{
    [ImplementsBusinessRule("USER_AUTH")]
    public void ValidateAuth() { }
}

public class MyService
{
    [BusinessRule("USER_AUTH")]  // ✅ Matches validator in Validators class
    public void DoSomething() { }
}
```

### Invalid: No Matching Validator

```csharp
public class MyService
{
    [BusinessRule("PAYMENT_VERIFIED")]  // ❌ BR002: No validator found
    public void ProcessPayment() { }
}
```

## Limitations

1. **Same Compilation Only**: The analyzer only recognizes validators defined in the current compilation. Validators in
   referenced assemblies are not detected.

2. **No Call Graph Analysis**: The analyzer doesn't check if the validator method is actually *called* by the requiring
   method. It only checks for the *presence* of a matching validator anywhere in the compilation.

3. **Reflection Scenarios**: For code that uses reflection or dynamic dispatch, the analyzer cannot trace which
   validators are invoked at runtime. Ensure explicit validator declarations exist.

## Future Enhancements

- **Call Graph Analysis**: Track whether validators are reachable through the call graph from requiring methods
- **Cross-Assembly Support**: Recognize validators defined in referenced assemblies
- **Scoped Validation**: Support namespace or assembly-level validator scoping
