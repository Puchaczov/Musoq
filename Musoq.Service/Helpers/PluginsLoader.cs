using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FQL.Schema;

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
                plugins.Add((ISchema)Activator.CreateInstance(assemblyType));

            return plugins.ToArray();
        }

        private static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var assembly = Assembly.GetEntryAssembly();
            var fileInfo = new FileInfo(assembly.Location);

            if (fileInfo.Directory == null)
                return new List<Assembly>();

            var thisDir = fileInfo.Directory.FullName;
            var pluginsDir = new DirectoryInfo(Path.Combine(thisDir, ApplicationConfiguration.PluginsFolder, ApplicationConfiguration.SchemasFolder));

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