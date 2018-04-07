using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Musoq.Schema;
using Musoq.Schema.Helpers;
using Musoq.Service.Logging;

namespace Musoq.Service.Helpers
{
    public static class PluginsLoader
    {
        private static ISchema[] _plugins;

        public static ISchema[] LoadSchemas()
        {
            if (_plugins != null)
                return _plugins;

            var assemblies = PluginsHelper.GetReferencingAssemblies(ApplicationConfiguration.PluginsFolder);
            var assemblyTypes = assemblies.SelectMany(assembly =>
                assembly.GetTypes());

            var interfaceType = typeof(ISchema);

            assemblyTypes = assemblyTypes.Where(type => interfaceType.IsAssignableFrom(type));

            var plugins = new List<ISchema>();

            foreach (var assemblyType in assemblyTypes)
            {
                try
                {
                    plugins.Add((ISchema)Activator.CreateInstance(assemblyType));
                }
                catch (Exception e)
                {
                    ServiceLogger.Instance.Log(e);
                }
            }

            _plugins = plugins.ToArray();
            return _plugins;
        }
    }
}