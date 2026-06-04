using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace BusinessRules.Wcf;

/// <summary>
/// A WCF-compatible fault detail carrying a violated <see cref="BusinessRuleBase" />.
/// Use with <c>FaultException&lt;BusinessRuleFault&gt;</c> for service boundary communication.
/// </summary>
[DataContract]
public record BusinessRuleFault(
    [property: DataMember]
    [property: Required]
    BusinessRuleBase BusinessRule);
