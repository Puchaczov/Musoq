namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteSidecarIndexSpec(
    string CteName,
    CteSidecarIndexKind Kind,
    string[] KeyColumns,
    Type KeyType,
    int IndexSlot)
{
    public string StableKey => CteSidecarIndexPlanner.CreateStableKey(CteName, Kind, KeyColumns);
}
