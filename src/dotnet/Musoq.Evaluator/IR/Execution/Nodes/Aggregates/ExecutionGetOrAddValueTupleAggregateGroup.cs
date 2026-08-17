using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGetOrAddValueTupleAggregateGroup : ExecutionNode
{
    public ExecutionGetOrAddValueTupleAggregateGroup(
        ExecutionVariable rootGroup,
        IReadOnlyList<AggregateGroupLookup> groupDictionaries,
        ExecutionVariable groupsToFinalize,
        ExecutionVariable group,
        IReadOnlyList<ExecutionExpression> keys,
        IReadOnlyList<string> keyNames,
        IReadOnlyList<ExecutionTypeRef> keyTypes,
        AggregateGroupPlan groupPlan)
    {
        RootGroup = rootGroup;
        GroupDictionaries = ExecutionIrCollections.Freeze(groupDictionaries);
        GroupsToFinalize = groupsToFinalize;
        Group = group;
        Keys = ExecutionIrCollections.Freeze(keys);
        KeyNames = ExecutionIrCollections.Freeze(keyNames);
        KeyTypes = ExecutionIrCollections.Freeze(keyTypes);
        GroupPlan = groupPlan;
    }

    public ExecutionVariable RootGroup { get; init; }
    public IReadOnlyList<AggregateGroupLookup> GroupDictionaries { get; init; }
    public ExecutionVariable GroupsToFinalize { get; init; }
    public ExecutionVariable Group { get; init; }
    public IReadOnlyList<ExecutionExpression> Keys { get; init; }
    public IReadOnlyList<string> KeyNames { get; init; }
    public IReadOnlyList<ExecutionTypeRef> KeyTypes { get; init; }
    public AggregateGroupPlan GroupPlan { get; init; }

    internal ExecutionGetOrAddValueTupleAggregateGroup(
        ExecutionVariable rootGroup,
        IReadOnlyList<AggregateGroupLookup> groupDictionaries,
        ExecutionVariable groupsToFinalize,
        ExecutionVariable group,
        IReadOnlyList<ExecutionExpression> keys,
        IReadOnlyList<string> keyNames,
        IReadOnlyList<Type> keyTypes,
        AggregateGroupPlan groupPlan)
        : this(rootGroup, groupDictionaries, groupsToFinalize, group, keys, keyNames, ExecutionClrBindingFactory.FromClrTypes(keyTypes), groupPlan)
    {
    }

    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
