namespace Musoq.Evaluator.IR.Planning;

internal sealed record ParallelSourceShapeResolution(PlanningRowShape? SourceShape, string Reason)
{
    public bool IsResolved => SourceShape != null;

    public static ParallelSourceShapeResolution Resolved(PlanningRowShape sourceShape)
    {
        return new ParallelSourceShapeResolution(sourceShape, string.Empty);
    }

    public static ParallelSourceShapeResolution Unresolved(string reason)
    {
        return new ParallelSourceShapeResolution(null, reason);
    }
}
