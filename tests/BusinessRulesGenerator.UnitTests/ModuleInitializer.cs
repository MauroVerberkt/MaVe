using System.Runtime.CompilerServices;

namespace BusinessRulesGenerator.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
