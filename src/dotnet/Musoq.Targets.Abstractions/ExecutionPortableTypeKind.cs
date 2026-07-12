namespace Musoq.Targets.Abstractions;

public enum ExecutionPortableTypeKind
{
    Primitive,
    Nullable,
    Array,
    Sequence,
    List,
    Map,
    Set,
    Pair,
    GeneratedRow,
    GenericParameter,
    ByRef,
    HostOpaque,
    ClrOnly
}
