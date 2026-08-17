using System.Reflection;
using System.Runtime.Loader;

namespace Musoq.Evaluator.Build;

internal sealed class LoadedAssemblyHandle : IDisposable
{
    private readonly AssemblyLoadContext? _loadContext;
    private bool _disposed;

    public LoadedAssemblyHandle(Assembly assembly, AssemblyLoadContext? loadContext)
    {
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _loadContext = loadContext;
    }

    public Assembly Assembly { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _loadContext?.Unload();
    }
}
