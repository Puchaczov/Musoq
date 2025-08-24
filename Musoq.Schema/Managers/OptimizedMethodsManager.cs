using System.Linq;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Managers;

/// <summary>
/// Optimized methods manager that uses compiled expression trees for improved performance.
/// Inherits from OptimizedMethodsMetadata to provide compiled method delegates.
/// </summary>
public class OptimizedMethodsManager : OptimizedMethodsMetadata
{
    /// <summary>
    /// Registers libraries using the optimized metadata system.
    /// Methods will be available for both reflection-based and compiled invocation.
    /// </summary>
    /// <param name="library">The library containing methods to register</param>
    public void RegisterLibraries(LibraryBase library)
    {
        var type = library.GetType();
        var methods = type.GetMethods().Where(f => f.GetCustomAttribute<BindableMethodAttribute>() != null);

        foreach (var methodInfo in methods)
            RegisterMethod(methodInfo);
    }
}