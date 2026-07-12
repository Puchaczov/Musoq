namespace Musoq.Targets.Abstractions;

public enum ExecutionPortableContainerKind
{
    Sequence,
    List,
    Map,
    Set,
    Pair
}

public sealed record ExecutionPortableContainerContract(
    ExecutionPortableContainerKind Kind,
    bool IsOrdered,
    bool IsMutable,
    bool RequiresKeyEquality,
    bool RequiresKeyHashing);
