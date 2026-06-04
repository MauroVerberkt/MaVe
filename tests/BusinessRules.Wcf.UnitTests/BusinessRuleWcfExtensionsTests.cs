using System.ServiceModel;

namespace BusinessRules.Wcf.UnitTests;

[TestFixture]
public class BusinessRuleWcfExtensionsTests
{
    [Test]
    public void ToFaultException_Generic_CreatesFaultExceptionWithCorrectDetails()
    {
        // Act
        var faultException = BusinessRuleWcfExtensions.ToFaultException<TestUserMustBeAdult>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(faultException, Is.Not.Null);
            Assert.That(faultException, Is.InstanceOf<FaultException<BusinessRuleFault>>());
            Assert.That(faultException.Detail, Is.Not.Null);
            Assert.That(faultException.Detail.BusinessRule, Is.Not.Null);
            Assert.That(faultException.Detail.BusinessRule.InternalKey, Is.EqualTo("TEST_USER_AGE_MIN"));
            Assert.That(faultException.Detail.BusinessRule.InternalRequirement,
                Is.EqualTo("User must be at least 18 years old"));
            Assert.That(faultException.Reason.ToString(), Is.EqualTo("User must be at least 18 years old"));
            Assert.That(faultException.Code.Name, Is.EqualTo("TEST_USER_AGE_MIN"));
        });
    }

    [Test]
    public void ToFaultException_Generic_WithDifferentRule_CreatesFaultExceptionWithCorrectDetails()
    {
        // Act
        var faultException = BusinessRuleWcfExtensions.ToFaultException<TestPasswordMinLength>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(faultException.Detail.BusinessRule.InternalKey, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
            Assert.That(faultException.Detail.BusinessRule.InternalRequirement,
                Is.EqualTo("Password must contain at least 8 characters"));
            Assert.That(faultException.Reason.ToString(), Is.EqualTo("Password must contain at least 8 characters"));
            Assert.That(faultException.Code.Name, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
        });
    }

    [Test]
    public void ToFaultException_FromBusinessRuleBase_CreatesFaultExceptionWithCorrectDetails()
    {
        // Arrange
        var businessRule = new TestUserMustBeAdult();

        // Act
        var faultException = businessRule.ToFaultException();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(faultException, Is.Not.Null);
            Assert.That(faultException.Detail.BusinessRule, Is.SameAs(businessRule));
            Assert.That(faultException.Detail.BusinessRule.InternalKey, Is.EqualTo("TEST_USER_AGE_MIN"));
            Assert.That(faultException.Reason.ToString(), Is.EqualTo("User must be at least 18 years old"));
            Assert.That(faultException.Code.Name, Is.EqualTo("TEST_USER_AGE_MIN"));
        });
    }

    [Test]
    public void ToFaultException_FromBusinessRuleBase_WithNullRule_ThrowsArgumentNullException()
    {
        // Arrange
        BusinessRuleBase? businessRule = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = businessRule!.ToFaultException());
    }

    [Test]
    public void ToFaultException_FromBusinessRuleViolationException_CreatesFaultExceptionWithCorrectDetails()
    {
        // Arrange
        var exception = TestUserMustBeAuthenticated.ToException();

        // Act
        var faultException = exception.ToFaultException();

        // Assert
        Assert.That(faultException, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(faultException.Detail.BusinessRule.InternalKey, Is.EqualTo("TEST_USER_AUTH"));
            Assert.That(faultException.Detail.BusinessRule.InternalRequirement,
                Is.EqualTo("User must be authenticated"));
            Assert.That(faultException.Reason.ToString(), Is.EqualTo("User must be authenticated"));
            Assert.That(faultException.Code.Name, Is.EqualTo("TEST_USER_AUTH"));
        });
    }

    [Test]
    public void ToFaultException_FromBusinessRuleViolationException_WithCustomMessage_CreatesFaultException()
    {
        // Arrange
        const string customMessage = "Custom authentication error message";
        var exception = TestUserMustBeAuthenticated.ToException(customMessage);

        // Act
        var faultException = exception.ToFaultException();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(faultException, Is.Not.Null);
            Assert.That(faultException.Detail.BusinessRule.InternalKey, Is.EqualTo("TEST_USER_AUTH"));

            // Note: The custom message is stored in the exception, not the business rule
            Assert.That(exception.Message, Is.EqualTo(customMessage));
        });
    }

    [Test]
    public void ToFaultException_FromBusinessRuleViolationException_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        BusinessRuleViolationException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = exception!.ToFaultException());
    }

    [Test]
    public void BusinessRuleFault_ContainsBusinessRule()
    {
        // Arrange
        var businessRule = new TestPasswordMinLength();

        // Act
        var fault = new BusinessRuleFault(businessRule);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(fault.BusinessRule, Is.SameAs(businessRule));
            Assert.That(fault.BusinessRule.InternalKey, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
            Assert.That(fault.BusinessRule.InternalDescription,
                Is.EqualTo("Passwords must meet minimum security requirements"));
            Assert.That(fault.BusinessRule.InternalCategory, Is.EqualTo("TestSecurity"));
        });
    }

    [Test]
    public void FaultException_CanBeThrown()
    {
        // Arrange
        var faultException = BusinessRuleWcfExtensions.ToFaultException<TestUserMustBeAdult>();

        // Act & Assert
        var ex = Assert.Throws<FaultException<BusinessRuleFault>>(() => throw faultException);

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Detail.BusinessRule.InternalKey, Is.EqualTo("TEST_USER_AGE_MIN"));
    }

    [Test]
    public void FaultException_CanBeCaught()
    {
        // Arrange
        var faultException = BusinessRuleWcfExtensions.ToFaultException<TestPasswordMinLength>();

        // Act
        BusinessRuleFault? caughtFault;
        try
        {
            throw faultException;
        }
        catch (FaultException<BusinessRuleFault> ex)
        {
            caughtFault = ex.Detail;
        }

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(caughtFault, Is.Not.Null);
            Assert.That(caughtFault.BusinessRule.InternalKey, Is.EqualTo("TEST_PWD_MIN_LENGTH"));
            Assert.That(caughtFault.BusinessRule.InternalRequirement,
                Is.EqualTo("Password must contain at least 8 characters"));
        });
    }
}
