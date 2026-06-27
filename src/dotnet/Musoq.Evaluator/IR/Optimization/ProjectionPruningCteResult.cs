using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.PhysicalColumnUsageFacts;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record ProjectionPruningCteResult(
    PhysicalCteNode Node,
    int PrunedFields,
    int RewrittenDefinitions)
{
    public static ProjectionPruningCteResult NoChange(PhysicalCteNode cte)
    {
        return new ProjectionPruningCteResult(cte, PrunedFields: 0, RewrittenDefinitions: 0);
    }
}
