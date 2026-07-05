using MaVe.BusinessRulesGenerator.UnitTests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MaVe.BusinessRulesGenerator.UnitTests;

[TestFixture]
public class BusinessRuleSourceGeneratorTests
{
    private static GeneratorDriver CreateDriver(string fileName, string jsonContent)
    {
        var generator = new BusinessRuleSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts([new InMemoryAdditionalText(fileName, jsonContent)]);
        return driver;
    }

    private static CSharpCompilation CreateCompilation(string assemblyName = "TestAssembly")
    {
        return CSharpCompilation.Create(
            assemblyName,
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Test]
    public Task SingleRule_SingleCategory_GeneratesCorrectSource()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "UserMustBeAuthenticated",
                                  "key": "USER_AUTH",
                                  "requirement": "User must be authenticated",
                                  "description": "Validates that the user is authenticated",
                                  "category": "Authentication"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        return Verify(driver);
    }

    [Test]
    public Task MultipleCategories_GeneratesSeparateFiles()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "UserMustBeAuthenticated",
                                  "key": "USER_AUTH",
                                  "requirement": "User must be authenticated",
                                  "description": "Validates that the user is authenticated",
                                  "category": "Authentication"
                                },
                                {
                                  "className": "UserMustBeAdmin",
                                  "key": "USER_ADMIN",
                                  "requirement": "User must have admin privileges",
                                  "description": "Validates that the user has admin rights",
                                  "category": "Authorization"
                                },
                                {
                                  "className": "AgeMinimum",
                                  "key": "AGE_MIN",
                                  "requirement": "Age minimum",
                                  "description": "User must meet minimum age requirement",
                                  "category": "Validation"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        return Verify(driver);
    }

    [Test]
    public Task EmptyCategory_DefaultsToGeneral()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "GenericRule",
                                  "key": "GENERIC",
                                  "requirement": "A generic rule",
                                  "description": "Has no category",
                                  "category": ""
                                },
                                {
                                  "className": "NullCategoryRule",
                                  "key": "NULL_CAT",
                                  "requirement": "Null category rule",
                                  "description": "Category is whitespace only",
                                  "category": "   "
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        return Verify(driver);
    }

    [Test]
    public void EmptyRulesArray_GeneratesNothing()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": []
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        var result = driver.GetRunResult();

        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [Test]
    public void NullRulesProperty_GeneratesNothing()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": null
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        var result = driver.GetRunResult();

        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [Test]
    public void InvalidJson_GeneratesNothing_NoCrash()
    {
        const string json = "{ this is not valid json at all }}}}";

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        var result = driver.GetRunResult();

        Assert.That(result.GeneratedTrees, Is.Empty);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public Task SpecialCharactersInStrings_AreEscaped()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "RuleWithSpecialChars",
                                  "key": "SPECIAL",
                                  "requirement": "Must contain \"quotes\" and \\backslashes",
                                  "description": "Line1\nLine2\rLine3",
                                  "category": "Escaping"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        return Verify(driver);
    }

    [Test]
    public Task InvalidCharactersInCategory_AreSanitized()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "SanitizedRule",
                                  "key": "SANITIZED",
                                  "requirement": "Category has special chars",
                                  "description": "Tests namespace sanitization",
                                  "category": "My Category!"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        return Verify(driver);
    }

    [Test]
    public void DigitLeadingCategory_PrefixedWithUnderscore()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "DigitCategoryRule",
                                  "key": "DIGIT_CAT",
                                  "requirement": "Category starts with digit",
                                  "description": "Tests namespace sanitization for leading digits",
                                  "category": "1Orders"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        var generated = driver.GetRunResult().GeneratedTrees.Single().GetText().ToString();

        Assert.That(generated, Does.Contain("namespace TestAssembly.BusinessRules.Rules._1Orders;"));
        Assert.That(generated, Does.Contain("public const string Category = \"1Orders\";"));
    }

    [Test]
    public Task AssemblyName_UsedInNamespace()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "SimpleRule",
                                  "key": "SIMPLE",
                                  "requirement": "Simple rule",
                                  "description": "Tests assembly name in namespace",
                                  "category": "Core"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation("MyCompany.MyApp"));

        return Verify(driver);
    }

    [Test]
    public void NonMatchingFileName_IsIgnored()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "className": "ShouldNotGenerate",
                                  "key": "IGNORED",
                                  "requirement": "Should be ignored",
                                  "description": "File name does not end in MaVe.BusinessRules.json",
                                  "category": "Test"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("rules.json", json)
            .RunGenerators(CreateCompilation());

        var result = driver.GetRunResult();

        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [Test]
    public Task CaseInsensitiveJsonProperties()
    {
        const string json = """
                            {
                              "MaVe.BusinessRules": [
                                {
                                  "ClassName": "PascalCaseRule",
                                  "Key": "PASCAL",
                                  "Requirement": "PascalCase properties work",
                                  "Description": "Tests case-insensitive deserialization",
                                  "Category": "CaseTest"
                                }
                              ]
                            }
                            """;

        var driver = CreateDriver("MaVe.BusinessRules.json", json)
            .RunGenerators(CreateCompilation());

        return Verify(driver);
    }
}
