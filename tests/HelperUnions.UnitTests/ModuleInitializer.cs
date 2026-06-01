using System.Runtime.CompilerServices;

namespace HelperUnions.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
