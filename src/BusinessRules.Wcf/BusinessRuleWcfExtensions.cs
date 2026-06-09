using System.Diagnostics.Contracts;
using System.ServiceModel;

namespace MaVe.BusinessRules.Wcf;

/// <summary>
/// Extension methods for integrating MaVe.BusinessRules with WCF (Windows Communication Foundation).
/// </summary>
public static class BusinessRuleWcfExtensions
{
    /// <summary>
    /// Creates a <see cref="FaultException{BusinessRuleFault}" /> from a <see cref="BusinessRule{T}" />.
    /// This is used in WCF scenarios to transmit business rule violations as structured faults.
    /// </summary>
    /// <typeparam name="T">The type of the business rule.</typeparam>
    /// <returns>
    /// A <see cref="FaultException{BusinessRuleFault}" /> containing the business rule information.
    /// </returns>
    /// <remarks>
    /// This method creates a new instance of the business rule and wraps it in a WCF fault exception.
    /// The fault reason is set to the rule's requirement text, and the fault code is set to the rule's key.
    /// </remarks>
    [Pure]
    public static FaultException<BusinessRuleFault> ToFaultException<T>()
        where T : BusinessRule<T>, new()
    {
        var instance = new T();
        return new FaultException<BusinessRuleFault>(
            new BusinessRuleFault(instance),
            new FaultReason(instance.InternalRequirement),
            new FaultCode(instance.InternalKey));
    }

    /// <summary>
    /// Creates a <see cref="FaultException{BusinessRuleFault}" /> from a <see cref="BusinessRuleBase" /> instance.
    /// This is used in WCF scenarios to transmit business rule violations as structured faults.
    /// </summary>
    /// <param name="businessRule">The business rule to convert to a fault exception.</param>
    /// <returns>
    /// A <see cref="FaultException{BusinessRuleFault}" /> containing the business rule information.
    /// </returns>
    /// <remarks>
    /// The fault reason is set to the rule's requirement text, and the fault code is set to the rule's key.
    /// </remarks>
    [Pure]
    public static FaultException<BusinessRuleFault> ToFaultException(this BusinessRuleBase businessRule)
    {
        ArgumentNullException.ThrowIfNull(businessRule);

        return new FaultException<BusinessRuleFault>(
            new BusinessRuleFault(businessRule),
            new FaultReason(businessRule.InternalRequirement),
            new FaultCode(businessRule.InternalKey));
    }

    /// <summary>
    /// Creates a <see cref="FaultException{BusinessRuleFault}" /> from a <see cref="BusinessRuleViolationException" />.
    /// This is used in WCF scenarios to transmit business rule violations as structured faults.
    /// </summary>
    /// <param name="exception">The business rule violation exception to convert.</param>
    /// <returns>
    /// A <see cref="FaultException{BusinessRuleFault}" /> containing the business rule information from the exception.
    /// </returns>
    [Pure]
    public static FaultException<BusinessRuleFault> ToFaultException(this BusinessRuleViolationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.BusinessRule.ToFaultException();
    }
}
