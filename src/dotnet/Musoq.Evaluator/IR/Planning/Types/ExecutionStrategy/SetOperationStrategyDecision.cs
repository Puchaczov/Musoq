namespace Musoq.Evaluator.IR.Planning;

internal sealed record SetOperationStrategyDecision(
    SetOperationLoweringKind LoweringKind,
    SetOperationTableStrategy TableStrategy,
    string Reason)
{
    public bool CanStreamUnionAll => LoweringKind == SetOperationLoweringKind.StreamingUnionAll;

    public string Outcome => CanStreamUnionAll ? LoweringKind.ToString() : TableStrategy.ToString();

    public static SetOperationStrategyDecision StreamingUnionAll(string reason)
    {
        return new SetOperationStrategyDecision(
            SetOperationLoweringKind.StreamingUnionAll,
            SetOperationTableStrategy.AppendLoop,
            reason);
    }

    public static SetOperationStrategyDecision AppendLoop(string reason)
    {
        return new SetOperationStrategyDecision(
            SetOperationLoweringKind.TableOperation,
            SetOperationTableStrategy.AppendLoop,
            reason);
    }

    public static SetOperationStrategyDecision HashSet(string reason)
    {
        return new SetOperationStrategyDecision(
            SetOperationLoweringKind.TableOperation,
            SetOperationTableStrategy.HashSet,
            reason);
    }

    public static SetOperationStrategyDecision GeneratedEqualityLoop(string reason)
    {
        return new SetOperationStrategyDecision(
            SetOperationLoweringKind.TableOperation,
            SetOperationTableStrategy.GeneratedEqualityLoop,
            reason);
    }
}
