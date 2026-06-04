using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BusinessRules;

/// <summary>
/// Generic base class for business rules using the Curiously Recurring Template Pattern (CRTP).
/// Derive from this to define concrete business rules with static access to rule metadata.
/// </summary>
/// <typeparam name="T">The concrete business rule type (self-referencing).</typeparam>
[DataContract]
[SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Intentional CRTP pattern: static members are per concrete BusinessRule type.")]
public abstract class BusinessRule<T>(string key, string requirement, string description = "", string category = "")
    : BusinessRuleBase
    where T : BusinessRule<T>, new()
{
    [DataMember]
    [Required]
    internal override string InternalKey { get; } = key;

    [DataMember]
    [Required]
    internal override string InternalRequirement { get; } = requirement;

    [DataMember]
    internal override string InternalDescription { get; } = description;

    [DataMember]
    internal override string InternalCategory { get; } = category;

    /// <summary>
    /// Creates a <see cref="BusinessRuleViolationException" /> for this rule using the requirement text as the message.
    /// </summary>
    public static BusinessRuleViolationException ToException()
    {
        return new BusinessRuleViolationException(new T());
    }

    /// <summary>
    /// Creates a <see cref="BusinessRuleViolationException" /> for this rule with a custom message.
    /// </summary>
    public static BusinessRuleViolationException ToException(string message)
    {
        return new BusinessRuleViolationException(new T(), message);
    }
}
