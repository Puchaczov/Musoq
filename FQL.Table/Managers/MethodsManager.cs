using System.Linq;
using System.Reflection;
using FQL.Plugins;
using FQL.Plugins.Attributes;

namespace FQL.Schema.Managers
{
    public class MethodsManager : MethodsMetadatas
    {
        public void RegisterLibraries(LibraryBase library)
        {
            var type = library.GetType();
            var methods = type.GetMethods().Where(f => f.GetCustomAttribute<BindableMethodAttribute>() != null);

            foreach (var methodInfo in methods)
                RegisterMethod(methodInfo);
        }
    }
}