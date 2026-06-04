namespace BusinessRules.Attributes;

/// <summary>
/// Marks a method or class as implementing validation for a specific business rule.
/// Satisfies the <see cref="BusinessRuleAttribute" /> enforcement check (BR002/BR003).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class ImplementsBusinessRuleAttribute(string ruleKey) : Attribute
{
    /// <summary>
    /// The unique key identifying the business rule being implemented.
    /// </summary>
    public string RuleKey { get; } = ruleKey;
}
