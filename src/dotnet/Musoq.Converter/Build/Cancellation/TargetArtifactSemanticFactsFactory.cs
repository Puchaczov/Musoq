using System.Threading;
using System.Linq;

namespace Musoq.Converter.Build;

internal static class TargetArtifactSemanticFactsFactory
{
    public static TargetArtifactSemanticFacts From(BuildItems items)
    {
        return From(items, CancellationToken.None);
    }

    public static TargetArtifactSemanticFacts From(
        BuildItems items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var facts = new TargetArtifactSemanticFacts(
            items.QueryResultMode,
            items.OutputType,
            items.ScriptParameterDefinitions,
            items.ScriptVariableDefinitions,
            items.UsedColumns,
            items.PipelineInferredColumns,
            items.SourcePlanRequestsPerSchema,
            cancellationToken,
            items.PlanningResult?.Facts.SourcePlanning.SourceDescriptorsBySourceId
                .ToDictionary(static source => source.Key, static source => source.Value.ExecutionPermissions));

        cancellationToken.ThrowIfCancellationRequested();
        return facts;
    }
}
