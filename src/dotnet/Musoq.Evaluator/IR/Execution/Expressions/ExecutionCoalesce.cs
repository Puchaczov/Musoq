using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCoalesce : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _expressions = [];

    public ExecutionCoalesce(
        IReadOnlyList<ExecutionExpression> expressions,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Expressions = ExecutionIrCollections.Freeze(expressions);
        ReturnType = returnType;
    }

    public IReadOnlyList<ExecutionExpression> Expressions
    {
        get => _expressions;
        init => _expressions = ExecutionIrCollections.Freeze(value);
    }

    public override ExecutionTypeRef ReturnType { get; init; }

    internal ExecutionCoalesce(IReadOnlyList<ExecutionExpression> expressions, Type returnType)
        : this(expressions, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
