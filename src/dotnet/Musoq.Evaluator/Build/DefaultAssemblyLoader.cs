using System.Reflection;

namespace Musoq.Evaluator.Build;

internal sealed class DefaultAssemblyLoader : IAssemblyLoader
{
    public static DefaultAssemblyLoader Instance { get; } = new();

    public Assembly Load(byte[] assemblyBytes)
    {
        return Assembly.Load(assemblyBytes);
    }
}
