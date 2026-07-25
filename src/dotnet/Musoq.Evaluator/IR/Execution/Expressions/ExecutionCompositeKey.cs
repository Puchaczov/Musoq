using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCompositeKey : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _parts = [];

    public ExecutionCompositeKey(IReadOnlyList<ExecutionExpression> parts)
        : base(ExecutionClrBindingFactory.FromClr(typeof(object)))
    {
        Parts = ExecutionIrCollections.Freeze(parts);
    }

    public IReadOnlyList<ExecutionExpression> Parts
    {
        get => _parts;
        init => _parts = ExecutionIrCollections.Freeze(value);
    }
}
