using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionContextArray : ExecutionExpression
{
    private IReadOnlyList<ExecutionContextSegment> _segments = [];

    public ExecutionContextArray(IReadOnlyList<ExecutionContextSegment> segments)
        : base(ExecutionClrBindingFactory.FromClr(typeof(object[])))
    {
        Segments = ExecutionIrCollections.Freeze(segments);
    }

    public IReadOnlyList<ExecutionContextSegment> Segments
    {
        get => _segments;
        init => _segments = ExecutionIrCollections.Freeze(value);
    }
}
