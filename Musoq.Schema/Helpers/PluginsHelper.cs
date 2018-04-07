using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Musoq.Schema.Helpers
{
    public static class PluginsHelper
    {
        public static IEnumerable<Assembly> GetReferencingAssemblies(string pluginsFolder)
        {
            var assembly = Assembly.GetEntryAssembly();
            var fileInfo = new FileInfo(assembly.Location);

            if (fileInfo.Directory == null)
                return new List<Assembly>();

            var thisDir = fileInfo.Directory.FullName;
            var pluginsDir = new DirectoryInfo(Path.Combine(thisDir, pluginsFolder));

            return pluginsDir
                .GetDirectories()
                .SelectMany(sm => sm
                    .GetFiles("*.dll")
                    .Select(file => Assembly.LoadFile(file.FullName)));
        }
    }
}