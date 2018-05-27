using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.Helpers;

namespace Musoq.Service.Core.Helpers
{
    public static class PluginsLoader
    {
        private static Type[] _plugins;

        public static Type[] LoadDllBasedSchemas()
        {
            if (_plugins != null)
                return _plugins;

            var assemblies = PluginsHelper.GetReferencingAssemblies(ApplicationConfiguration.PluginsFolder);
            var assemblyTypes = assemblies.SelectMany(assembly =>
                assembly.GetTypes());

            var interfaceType = typeof(ISchema);

            _plugins = assemblyTypes
                .Where(type => interfaceType.IsAssignableFrom(type) && type.HasParameterlessConstructor()).ToArray();

            return _plugins;
        }

        public static bool HasParameterlessConstructor(this Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}