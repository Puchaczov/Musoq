namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private HashJoinBuildContext PruneFusedHashPayload(
        HashJoinBuildContext context,
        ExecutionBlock matchedBody)
    {
        if (context.Sides.Build.FusedHashPayload is not { } payload)
            return context;

        if (!CanApplyHashBuildRowWidthPruning())
            return context;

        if (!new SingleUseHashBuildFusionPlanner().TryPruneFusedHashPayload(
                payload,
                context.Sides.Build.Shapes,
                matchedBody,
                context.Sides.Build.Variable.Name,
                out var pruning))
        {
            return context;
        }

        var build = context.Sides.Build with
        {
            FusedHashPayload = pruning.Payload,
            Shapes = pruning.Shapes
        };
        var sides = context.Sides with { Build = build };
        var sources = ReplaceBuildSource(context.Sources, context.Sides.Build, build);
        return context with
        {
            Sources = sources,
            Sides = sides
        };
    }

    private static JoinSources ReplaceBuildSource(
        JoinSources sources,
        JoinSource oldBuild,
        JoinSource newBuild)
    {
        return string.Equals(sources.Left.Variable.Name, oldBuild.Variable.Name, StringComparison.Ordinal)
            ? sources with { Left = newBuild }
            : sources with { Right = newBuild };
    }
}
