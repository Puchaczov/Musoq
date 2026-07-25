using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionValueTupleKey : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _parts = [];

    public ExecutionValueTupleKey(
        IReadOnlyList<ExecutionExpression> parts,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Parts = ExecutionIrCollections.Freeze(parts);
        ReturnType = returnType;
    }

    public IReadOnlyList<ExecutionExpression> Parts
    {
        get => _parts;
        init => _parts = ExecutionIrCollections.Freeze(value);
    }

    public override ExecutionTypeRef ReturnType { get; init; }

    internal ExecutionValueTupleKey(IReadOnlyList<ExecutionExpression> parts, Type returnType)
        : this(parts, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
