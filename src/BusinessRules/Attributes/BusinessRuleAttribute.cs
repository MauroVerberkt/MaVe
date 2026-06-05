namespace MaVe.BusinessRules.Attributes;

/// <summary>
/// Marks a method or class as requiring a specific business rule to be enforced.
/// The analyzer verifies that a matching <see cref="ImplementsBusinessRuleAttribute" /> exists in the compilation.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class BusinessRuleAttribute(string ruleKey, bool enforceValidation = true) : Attribute
{
    /// <summary>
    /// The unique key identifying the required business rule.
    /// </summary>
    public string RuleKey { get; } = ruleKey;

    /// <summary>
    /// When <c>true</c>, a missing <see cref="ImplementsBusinessRuleAttribute" /> produces a compile error (BR002).
    /// When <c>false</c>, it produces a warning (BR003).
    /// </summary>
    public bool EnforceValidation { get; } = enforceValidation;
}
