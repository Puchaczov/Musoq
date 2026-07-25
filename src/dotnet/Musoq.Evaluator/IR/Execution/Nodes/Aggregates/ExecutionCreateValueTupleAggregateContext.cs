using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateValueTupleAggregateContext : ExecutionNode
{
    public ExecutionCreateValueTupleAggregateContext(
        ExecutionVariable rootGroup,
        IReadOnlyList<AggregateGroupLookup> groupDictionaries,
        ExecutionVariable groupsToFinalize,
        IReadOnlyList<ExecutionTypeRef> keyTypes,
        AggregateGroupPlan groupPlan)
    {
        RootGroup = rootGroup;
        GroupDictionaries = ExecutionIrCollections.Freeze(groupDictionaries);
        GroupsToFinalize = groupsToFinalize;
        KeyTypes = ExecutionIrCollections.Freeze(keyTypes);
        GroupPlan = groupPlan;
    }

    public ExecutionVariable RootGroup { get; init; }
    public IReadOnlyList<AggregateGroupLookup> GroupDictionaries { get; init; }
    public ExecutionVariable GroupsToFinalize { get; init; }
    public IReadOnlyList<ExecutionTypeRef> KeyTypes { get; init; }
    public AggregateGroupPlan GroupPlan { get; init; }

    internal ExecutionCreateValueTupleAggregateContext(
        ExecutionVariable rootGroup,
        IReadOnlyList<AggregateGroupLookup> groupDictionaries,
        ExecutionVariable groupsToFinalize,
        IReadOnlyList<Type> keyTypes,
        AggregateGroupPlan groupPlan)
        : this(rootGroup, groupDictionaries, groupsToFinalize, ExecutionClrBindingFactory.FromClrTypes(keyTypes), groupPlan)
    {
    }

    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
