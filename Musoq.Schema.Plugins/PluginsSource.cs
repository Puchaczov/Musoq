using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;
using Musoq.Service.Environment;

namespace Musoq.Schema.Plugins
{
    public class PluginsSource : RowSourceBase<PluginEntity>
    {
        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<PluginEntity>>> chunkedSource)
        {
            var chunk = new List<EntityResolver<PluginEntity>>();

            var pluginsFolder = EnvironmentServiceHelper.PluginsFolder;
            
            var assemblies = Helpers.PluginsHelper.GetReferencingAssemblies(pluginsFolder);
            var assemblyTypes = assemblies.SelectMany(assembly =>
                assembly.GetTypes());

            var interfaceType = typeof(ISchema);

            assemblyTypes = assemblyTypes.Where(type => interfaceType.IsAssignableFrom(type));
            
            foreach (var assemblyType in assemblyTypes)
            {
                try
                {
                    var schema = (ISchema) Activator.CreateInstance(assemblyType);
                    chunk.Add(new EntityResolver<PluginEntity>(new PluginEntity(assemblyType.Assembly, schema.Name), PluginsHelper.PluginNameToIndexMap, PluginsHelper.PluginIndexToMethodAccessMap));
                }
                catch (Exception e)
                {
                }
            }

            chunkedSource.Add(chunk);
        }
    }
}