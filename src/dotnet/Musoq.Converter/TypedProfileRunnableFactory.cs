using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

internal sealed class TypedProfileRunnableFactory<TOut>
{
    private readonly TypedRunnableFactoryCore<ITableRunnable> _core;

    public TypedProfileRunnableFactory(
        Type runnableType,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions,
        TypedQueryDiagnostics diagnostics,
        ILoggerResolver loggerResolver)
    {
        _core = new TypedRunnableFactoryCore<ITableRunnable>(
            runnableType,
            sourceRuntimeSettingsBySourceContextId,
            sourceRuntimeSettingDescriptionsBySourceContextId,
            sourceExecutionPlans,
            parameterDefinitions,
            diagnostics,
            loggerResolver);
    }

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _core.ParameterDefinitions;

    public IReadOnlyList<ScriptParameterContract> ParameterContracts => _core.ParameterContracts;

    internal Type RunnableType => _core.RunnableType;

    public TypedQueryDiagnostics Diagnostics => _core.Diagnostics;

    public CompiledTypedProfileQuery<TOut> Create(ISchemaProvider provider)
    {
        return new CompiledTypedProfileQuery<TOut>(this, provider);
    }

    internal CompiledQuery CreateCompiledQuery(ISchemaProvider provider)
    {
        return new CompiledQuery(_core.CreateRunnable(provider));
    }
}
