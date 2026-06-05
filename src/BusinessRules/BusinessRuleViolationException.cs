namespace MaVe.BusinessRules;

/// <summary>
/// Exception thrown when a business rule is violated.
/// Carries the associated <see cref="BusinessRuleBase" /> for inspection by callers.
/// </summary>
public class BusinessRuleViolationException : Exception
{
    /// <summary>
    /// Initializes a new instance using the requirement text as the exception message.
    /// </summary>
    public BusinessRuleViolationException(BusinessRuleBase businessRule)
        : base(businessRule.InternalRequirement)
    {
        BusinessRule = businessRule;
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    public BusinessRuleViolationException(BusinessRuleBase businessRule, string message)
        : base(message)
    {
        BusinessRule = businessRule;
    }

    /// <summary>
    /// Initializes a new instance with a custom message and inner exception.
    /// </summary>
    public BusinessRuleViolationException(BusinessRuleBase businessRule, string message, Exception innerException)
        : base(message, innerException)
    {
        BusinessRule = businessRule;
    }

    /// <summary>
    /// The business rule that was violated.
    /// </summary>
    public BusinessRuleBase BusinessRule { get; }

    /// <summary>
    /// The unique key identifying the violated rule.
    /// </summary>
    public string Key => BusinessRule.InternalKey;

    /// <summary>
    /// The human-readable requirement statement.
    /// </summary>
    public string Requirement => BusinessRule.InternalRequirement;

    /// <summary>
    /// A detailed description of the rule, if provided.
    /// </summary>
    public string Description => BusinessRule.InternalDescription;

    /// <summary>
    /// The category this rule belongs to.
    /// </summary>
    public string Category => BusinessRule.InternalCategory;
}
