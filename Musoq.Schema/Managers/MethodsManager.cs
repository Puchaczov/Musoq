using System.Linq;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Managers;

public class MethodsManager : MethodsMetadata
{
    public void RegisterLibraries(LibraryBase library)
    {
        var type = library.GetType();
        var methods = type.GetMethods().Where(f => f.GetCustomAttribute<BindableMethodAttribute>() != null);

        foreach (var methodInfo in methods)
            RegisterMethod(methodInfo);
    }
}