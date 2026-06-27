using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariablesSchema : SchemaBase
{
    private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

    public EnvironmentVariablesSchema()
        : base("environmentVariables", CachedLibrary.Value)
    {
        AddTable<EnvironmentVariableEntityTable>("all");
        AddSource<EnvironmentVariablesSource>("all");
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, EnvironmentVariableEntity>(name, new EnvironmentVariablesSource(executionContext));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();

        var lib = new EnvironmentVariablesLibrary();

        methodManager.RegisterLibraries(lib);

        return new MethodsAggregator(methodManager);
    }

    private sealed class EnvironmentVariablesSource(SourceExecutionContext runtimeContext) : RowSource<EnvironmentVariableEntity>
    {
        public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks =>
        [
            runtimeContext.SourceRuntimeSettings.Select(variable =>
                new EnvironmentVariableEntity(variable.Key, variable.Value)).ToArray()
        ];
    }
}
