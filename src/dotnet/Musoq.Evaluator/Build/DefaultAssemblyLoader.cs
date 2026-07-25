using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Musoq.Evaluator.Build;

internal sealed class DefaultAssemblyLoader : IAssemblyLoader
{
    public static DefaultAssemblyLoader Instance { get; } = new();

    public LoadedAssemblyHandle Load(byte[] assemblyBytes)
    {
        ArgumentNullException.ThrowIfNull(assemblyBytes);

        var loadContext = new CollectibleInterpreterAssemblyLoadContext();
        try
        {
            using var stream = new MemoryStream(assemblyBytes, writable: false);
            var assembly = loadContext.LoadFromStream(stream);
            return new LoadedAssemblyHandle(assembly, loadContext);
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    private sealed class CollectibleInterpreterAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleInterpreterAssemblyLoadContext()
            : base($"Musoq.Interpreter.{Guid.NewGuid():N}", isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            try
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (FileLoadException)
            {
                return null;
            }
        }
    }
}
