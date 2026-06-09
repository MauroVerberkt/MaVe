using System.Runtime.CompilerServices;

namespace MaVe.Railyard.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
