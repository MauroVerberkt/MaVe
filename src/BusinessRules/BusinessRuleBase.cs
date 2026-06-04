using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace BusinessRules;

/// <summary>
/// Abstract base class for all business rules, providing key, requirement, description, and category.
/// </summary>
[DataContract]
public abstract class BusinessRuleBase
{
    [DataMember]
    [Required]
    internal abstract string InternalKey { get; }

    [DataMember]
    [Required]
    internal abstract string InternalRequirement { get; }

    [DataMember]
    internal abstract string InternalDescription { get; }

    [DataMember]
    internal abstract string InternalCategory { get; }
}
