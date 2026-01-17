using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariablesSchema : SchemaBase
{
    private static readonly IReadOnlyDictionary<string, int> EnvironmentVariableNameToIndexMap;

    private static readonly IReadOnlyDictionary<int, Func<EnvironmentVariableEntity, object>>
        EnvironmentVariableIndexToObjectAccessMap;

    static EnvironmentVariablesSchema()
    {
        EnvironmentVariableNameToIndexMap = new Dictionary<string, int>
        {
            { nameof(EnvironmentVariableEntity.Key), 0 },
            { nameof(EnvironmentVariableEntity.Value), 1 }
        };

        EnvironmentVariableIndexToObjectAccessMap = new Dictionary<int, Func<EnvironmentVariableEntity, object>>
        {
            { 0, arg => arg.Key },
            { 1, arg => arg.Value }
        };
    }

    public EnvironmentVariablesSchema()
        : base("environmentVariables", CreateLibrary())
    {
        AddTable<EnvironmentVariableEntityTable>("all");
        AddSource<EnvironmentVariablesSource>("all");
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new EnvironmentVariablesSource(runtimeContext);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new EnvironmentVariablesLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }

    private class EnvironmentVariablesSource(RuntimeContext runtimeContext) : RowSource
    {
        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                return runtimeContext.EnvironmentVariables.Select(variable =>
                    new EntityResolver<EnvironmentVariableEntity>(
                        new EnvironmentVariableEntity(variable.Key, variable.Value),
                        EnvironmentVariableNameToIndexMap,
                        EnvironmentVariableIndexToObjectAccessMap));
            }
        }
    }
}