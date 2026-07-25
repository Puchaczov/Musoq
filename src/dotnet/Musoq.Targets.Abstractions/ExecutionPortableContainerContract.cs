namespace Musoq.Targets.Abstractions;

public enum ExecutionPortableContainerKind
{
    Sequence,
    List,
    Map,
    Set,
    Pair
}

public enum ExecutionPortableContainerBindingKind
{
    Canonical,
    Enumerable,
    ReadOnlyCollection,
    ReadOnlyList,
    Collection,
    ListInterface,
    List,
    ReadOnlyDictionary,
    DictionaryInterface,
    Dictionary,
    HashSet,
    KeyValuePair
}

public sealed record ExecutionPortableContainerContract(
    ExecutionPortableContainerKind Kind,
    bool IsOrdered,
    bool IsMutable,
    bool RequiresKeyEquality,
    bool RequiresKeyHashing,
    ExecutionPortableContainerBindingKind BindingKind = ExecutionPortableContainerBindingKind.Canonical);
