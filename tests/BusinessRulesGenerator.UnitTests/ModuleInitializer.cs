using System.Runtime.CompilerServices;

namespace MaVe.BusinessRulesGenerator.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
