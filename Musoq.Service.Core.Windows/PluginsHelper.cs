using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Musoq.Service.Core.Windows.Helpers
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

            if (!pluginsDir.Exists)
                pluginsDir.Create();

            return pluginsDir
                .GetDirectories()
                .SelectMany(sm => sm
                    .GetFiles(searchPattern));
        }

        public static IEnumerable<Assembly> GetReferencingAssemblies(string pluginsFolder)
        {
            var files = GetFilesFromPluginsFolder(pluginsFolder, "*.dll").ToArray();

            var assemblies = new List<Assembly>();
            foreach(var file in files)
            {
                AssemblyLoader loader = new AssemblyLoader(file.Directory.FullName);
                var assembly = loader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(file.FullName));
                assemblies.Add(assembly);
            }

            return assemblies;
        }

        public static IEnumerable<FileInfo> GetPythonSchemas(string pluginsFolder)
        {
            return GetFilesFromPluginsFolder(pluginsFolder, "*.py");
        }
    }

    public class AssemblyLoader : AssemblyLoadContext
    {
        private readonly string _folderPath;
        private readonly static string _mainFolderPath;

        public AssemblyLoader(string folderPath)
        {
            this._folderPath = folderPath;
        }

        static AssemblyLoader()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            _mainFolderPath = new FileInfo(executingAssembly.Location).DirectoryName;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            if (res.Count > 0)
            {
                return Assembly.Load(new AssemblyName(res.First().Name));
            }
            else
            {
                var apiApplicationFileInfo = new FileInfo($"{_folderPath}{Path.DirectorySeparatorChar}{assemblyName.Name}.dll");
                var mainFolderDllCandidate = new FileInfo(Path.Combine(_mainFolderPath, $"{assemblyName.Name}.dll"));
                if (File.Exists(apiApplicationFileInfo.FullName))
                {
                    var asl = new AssemblyLoader(apiApplicationFileInfo.DirectoryName);
                    return asl.LoadFromAssemblyPath(apiApplicationFileInfo.FullName);
                }
                else if (File.Exists(mainFolderDllCandidate.FullName))
                {
                    var asl = new AssemblyLoader(mainFolderDllCandidate.DirectoryName);
                    return asl.LoadFromAssemblyPath(mainFolderDllCandidate.FullName);
                }
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.FullName == assemblyName.FullName)
                    return assembly;

            return Assembly.Load(assemblyName);
        }
    }
}