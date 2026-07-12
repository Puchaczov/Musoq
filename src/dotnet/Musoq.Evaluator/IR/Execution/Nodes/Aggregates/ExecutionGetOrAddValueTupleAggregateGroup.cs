using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGetOrAddValueTupleAggregateGroup(
    ExecutionVariable RootGroup,
    IReadOnlyList<AggregateGroupLookup> GroupDictionaries,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable Group,
    IReadOnlyList<ExecutionExpression> Keys,
    IReadOnlyList<string> KeyNames,
    IReadOnlyList<ExecutionTypeRef> KeyTypes,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    internal ExecutionGetOrAddValueTupleAggregateGroup(
        ExecutionVariable rootGroup,
        IReadOnlyList<AggregateGroupLookup> groupDictionaries,
        ExecutionVariable groupsToFinalize,
        ExecutionVariable group,
        IReadOnlyList<ExecutionExpression> keys,
        IReadOnlyList<string> keyNames,
        IReadOnlyList<Type> keyTypes,
        AggregateGroupPlan groupPlan)
        : this(rootGroup, groupDictionaries, groupsToFinalize, group, keys, keyNames, ExecutionTypeRef.FromClrTypes(keyTypes), groupPlan)
    {
    }

    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
