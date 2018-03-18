using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Musoq.Schema;
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

            var assemblies = GetReferencingAssemblies();
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

        private static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var assembly = Assembly.GetEntryAssembly();
            var fileInfo = new FileInfo(assembly.Location);

            if (fileInfo.Directory == null)
                return new List<Assembly>();

            var thisDir = fileInfo.Directory.FullName;
            var pluginsDir = new DirectoryInfo(Path.Combine(thisDir, ApplicationConfiguration.PluginsFolder));

            return pluginsDir
                .GetDirectories()
                .SelectMany(sm => sm
                    .GetFiles("*.dll")
                    .Select(file => Assembly.LoadFile(file.FullName)));
        }
    }
}