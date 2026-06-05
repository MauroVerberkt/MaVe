using System.Runtime.CompilerServices;

namespace MaVe.Unions.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
