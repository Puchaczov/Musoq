using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

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
            SetOperationTableStrategy.RowComparer,
            reason);
    }

    public static SetOperationStrategyDecision HashSet(string reason)
    {
        return new SetOperationStrategyDecision(
            SetOperationLoweringKind.TableOperation,
            SetOperationTableStrategy.HashSet,
            reason);
    }

    public static SetOperationStrategyDecision RowComparer(string reason)
    {
        return new SetOperationStrategyDecision(
            SetOperationLoweringKind.TableOperation,
            SetOperationTableStrategy.RowComparer,
            reason);
    }
}
