using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Musoq.Schema;

namespace Musoq.Service.Helpers
{
    public static class PluginsLoader
    {
        public static ISchema[] LoadSchemas()
        {
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
                    if (Debugger.IsAttached)
                        Debug.Write(e);
                }
            }

            return plugins.ToArray();
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
                    .Select(file =>
                    {
                        return Assembly.LoadFile(file.FullName);
                    }));
        }
    }
}