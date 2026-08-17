namespace Musoq.Converter.Build;

internal static class TargetArtifactSemanticFactsFactory
{
    public static TargetArtifactSemanticFacts From(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new TargetArtifactSemanticFacts(
            items.QueryResultMode,
            items.OutputType,
            items.ScriptParameterDefinitions,
            items.ScriptVariableDefinitions,
            items.UsedColumns,
            items.PipelineInferredColumns,
            items.SourcePlanRequestsPerSchema);
    }
}
