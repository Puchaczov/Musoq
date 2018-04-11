using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Musoq.Schema.Helpers
{
    public static class PluginsHelper
    {
        private static IEnumerable<FileInfo> GetFilesFromPluginsFolder(string pluginsFolder, string searchPattern)
        {
            var assembly = Assembly.GetEntryAssembly();
            var fileInfo = new FileInfo(assembly.Location);

            if (fileInfo.Directory == null)
                return new List<FileInfo>();

            var thisDir = fileInfo.Directory.FullName;
            var pluginsDir = new DirectoryInfo(Path.Combine(thisDir, pluginsFolder));

            return pluginsDir
                .GetDirectories()
                .SelectMany(sm => sm
                    .GetFiles(searchPattern));
        }

        public static IEnumerable<Assembly> GetReferencingAssemblies(string pluginsFolder)
        {
            return GetFilesFromPluginsFolder(pluginsFolder, "*.dll").Select(f => Assembly.LoadFile(f.FullName));
        }

        public static IEnumerable<FileInfo> GetPythonSchemas(string pluginsFolder)
        {
            return GetFilesFromPluginsFolder(pluginsFolder, "*.py");
        }
    }
}