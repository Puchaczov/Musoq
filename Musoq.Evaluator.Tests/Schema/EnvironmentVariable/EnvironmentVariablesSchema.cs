using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable
{
    public class EnvironmentVariablesSchema : SchemaBase
    {
        private static readonly IDictionary<string, int> EnvironmentVariableNameToIndexMap;
        private static readonly IDictionary<int, Func<EnvironmentVariableEntity, object>> EnvironmentVariableIndexToObjectAccessMap;
        private readonly IEnumerable<EnvironmentVariableEntity> _sources;

        static EnvironmentVariablesSchema()
        {
            EnvironmentVariableNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(EnvironmentVariableEntity.Key), 0},
                {nameof(EnvironmentVariableEntity.Value), 1},
            };

            EnvironmentVariableIndexToObjectAccessMap = new Dictionary<int, Func<EnvironmentVariableEntity, object>>
            {
                {0, arg => arg.Key},
                {1, arg => arg.Value},
            };
        }

        public EnvironmentVariablesSchema(IEnumerable<EnvironmentVariableEntity> sources)
            : base("environmentVariables", CreateLibrary())
        {
            _sources = sources;
            
            AddSource<EntitySource<EnvironmentVariableEntity>>("all", sources, EnvironmentVariableNameToIndexMap, EnvironmentVariableIndexToObjectAccessMap);
            AddTable<EnvironmentVariableEntityTable>("all");
        }

        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new EnvironmentVariablesSource(runtimeContext);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var lib = new EnvironmentVariablesLibrary();

            propertiesManager.RegisterProperties(lib);
            methodManager.RegisterLibraries(lib);

            return new MethodsAggregator(methodManager, propertiesManager);
        }
        
        private class EnvironmentVariablesSource : RowSource
        {
            private readonly RuntimeContext _runtimeContext;
            
            public EnvironmentVariablesSource(RuntimeContext runtimeContext)
            {
                _runtimeContext = runtimeContext;
            }

            public override IEnumerable<IObjectResolver> Rows
            {
                get
                {
                    return _runtimeContext.EnvironmentVariables.Select(variable => new EntityResolver<EnvironmentVariableEntity>(
                        new EnvironmentVariableEntity(variable.Key, variable.Value), 
                        EnvironmentVariableNameToIndexMap, 
                        EnvironmentVariableIndexToObjectAccessMap));
                }
            }
        }
    }
}