namespace BusinessRules.UnitTests.TestHelpers;

/// <summary>
/// Provides generated BusinessRule source code for testing
/// </summary>
public static class GeneratedBusinessRules
{
    public const string UserMustBeAuthenticatedSource = """
                                                        namespace BusinessRules.Rules.Authentication
                                                        {
                                                            public class UserMustBeAuthenticated() : BusinessRules.BusinessRule<UserMustBeAuthenticated>(Key, Requirement, Description, Category)
                                                            {
                                                                public const string Key = "USER_AUTH";
                                                                public const string Requirement = "User must be authenticated";
                                                                public const string Description = "Validates that the user is authenticated";
                                                                public const string Category = "Authentication";
                                                            }
                                                        }
                                                        """;

    public const string UserMustBeAdminSource = """
                                                namespace BusinessRules.Rules.Authorization
                                                {
                                                    public class UserMustBeAdmin() : BusinessRules.BusinessRule<UserMustBeAdmin>(Key, Requirement, Description, Category)
                                                    {
                                                        public const string Key = "USER_ADMIN";
                                                        public const string Requirement = "User must have admin privileges";
                                                        public const string Description = "Validates that the user has admin rights";
                                                        public const string Category = "Authorization";
                                                    }
                                                }
                                                """;

    public static string GetAllGeneratedSources()
    {
        return UserMustBeAuthenticatedSource + "\n\n" + UserMustBeAdminSource;
    }
}
