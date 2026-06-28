using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

internal sealed class TypedRunnableFactory<TOut>
{
    private readonly TypedRunnableFactoryCore<ITypedRunnable<TOut>> _core;

    public TypedRunnableFactory(
        Type runnableType,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions,
        TypedQueryDiagnostics diagnostics,
        ILoggerResolver loggerResolver)
    {
        _core = new TypedRunnableFactoryCore<ITypedRunnable<TOut>>(
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

    public TypedQueryDiagnostics Diagnostics => _core.Diagnostics;

    public CompiledTypedQuery<TOut> Create(ISchemaProvider provider)
    {
        return new CompiledTypedQuery<TOut>(_core.CreateRunnable(provider));
    }
}
