using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record ParallelSourceShapeResolution(RowShape? SourceShape, string Reason)
{
    public bool IsResolved => SourceShape != null;

    public static ParallelSourceShapeResolution Resolved(RowShape sourceShape)
    {
        return new ParallelSourceShapeResolution(sourceShape, string.Empty);
    }

    public static ParallelSourceShapeResolution Unresolved(string reason)
    {
        return new ParallelSourceShapeResolution(null, reason);
    }
}
