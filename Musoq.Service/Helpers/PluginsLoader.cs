using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Service.Logging;

namespace Musoq.Service.Helpers
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

            _plugins = assemblyTypes.Where(type => interfaceType.IsAssignableFrom(type)).ToArray();

            return _plugins;
        }
    }
}