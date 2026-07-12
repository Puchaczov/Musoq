using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateValueTupleAggregateContext(
    ExecutionVariable RootGroup,
    IReadOnlyList<AggregateGroupLookup> GroupDictionaries,
    ExecutionVariable GroupsToFinalize,
    IReadOnlyList<ExecutionTypeRef> KeyTypes,
    AggregateGroupPlan GroupPlan) : ExecutionNode
{
    internal ExecutionCreateValueTupleAggregateContext(
        ExecutionVariable rootGroup,
        IReadOnlyList<AggregateGroupLookup> groupDictionaries,
        ExecutionVariable groupsToFinalize,
        IReadOnlyList<Type> keyTypes,
        AggregateGroupPlan groupPlan)
        : this(rootGroup, groupDictionaries, groupsToFinalize, ExecutionTypeRef.FromClrTypes(keyTypes), groupPlan)
    {
    }

    public AggregateGroupShape GroupShape => GroupPlan.LeafShape;
}
