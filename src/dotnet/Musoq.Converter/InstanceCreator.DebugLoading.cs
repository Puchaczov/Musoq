using System.Reflection;
using System.Runtime.Loader;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private sealed class DebugAssemblyLoadContext() : AssemblyLoadContext(true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
