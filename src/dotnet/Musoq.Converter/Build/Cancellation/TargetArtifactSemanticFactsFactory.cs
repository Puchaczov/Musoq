using System.Threading;

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
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return facts;
    }
}
