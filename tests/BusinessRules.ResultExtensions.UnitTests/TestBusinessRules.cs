namespace MaVe.BusinessRules.ResultExtensions.UnitTests;

// Test business rules for unit testing
public class TestUserMustBeAdult() : BusinessRule<TestUserMustBeAdult>(
    "TEST_USER_AGE_MIN",
    "User must be at least 18 years old",
    "Users under 18 cannot create accounts",
    "TestValidation")
{
}

public class TestPasswordMinLength() : BusinessRule<TestPasswordMinLength>(
    "TEST_PWD_MIN_LENGTH",
    "Password must contain at least 8 characters",
    "Passwords must meet minimum security requirements",
    "TestSecurity")
{
}

public class TestPasswordMustContainUppercase() : BusinessRule<TestPasswordMustContainUppercase>(
    "TEST_PWD_UPPERCASE",
    "Password must contain at least one uppercase letter",
    "Passwords must meet security requirements",
    "TestSecurity")
{
}

public class TestPasswordMustContainNumber() : BusinessRule<TestPasswordMustContainNumber>(
    "TEST_PWD_NUMBER",
    "Password must contain at least one number",
    "Passwords must meet security requirements",
    "TestSecurity")
{
}

public class TestUserMustBeAuthenticated() : BusinessRule<TestUserMustBeAuthenticated>(
    "TEST_USER_AUTH",
    "User must be authenticated",
    "User must provide valid authentication credentials",
    "TestAuthentication")
{
}

public class TestEmailMustBeValid() : BusinessRule<TestEmailMustBeValid>(
    "TEST_EMAIL_VALID",
    "Email address must be valid",
    "A valid email address is required",
    "TestValidation")
{
}
