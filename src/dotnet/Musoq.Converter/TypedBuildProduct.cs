using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

internal sealed record TypedBuildProduct(
    Type? RunnableType,
    QueryResultMode ResultMode,
    QueryMethodRenderMetadata RenderMetadata,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId,
    IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId,
    IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans,
    IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions,
    TypedQueryDiagnostics Diagnostics)
{
    public Type RequireRunnableType()
    {
        return RunnableType ?? throw new InvalidOperationException("Typed build product does not contain a runnable type.");
    }
}
