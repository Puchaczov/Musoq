using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal enum PredicateEarliestPlacement
{
    ConstantPredicate,
    SourcePushdown,
    SourceRuntimeFilter,
    PreInnerJoinLeft,
    PreInnerJoinRight,
    PostJoin,
    PostAggregate,
    PostWindow,
    RuntimeFilter
}
