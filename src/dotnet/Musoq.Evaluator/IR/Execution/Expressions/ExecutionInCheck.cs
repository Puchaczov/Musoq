using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInCheck : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _values = [];

    public ExecutionInCheck(
        ExecutionExpression expression,
        IReadOnlyList<ExecutionExpression> values,
        ExecutionTypeRef returnType,
        ExecutionConstantInSet? constantSet = null)
        : base(returnType)
    {
        Expression = expression;
        Values = ExecutionIrCollections.Freeze(values);
        ReturnType = returnType;
        ConstantSet = constantSet;
    }

    public ExecutionExpression Expression { get; init; }

    public IReadOnlyList<ExecutionExpression> Values
    {
        get => _values;
        init => _values = ExecutionIrCollections.Freeze(value);
    }

    public override ExecutionTypeRef ReturnType { get; init; }

    public ExecutionConstantInSet? ConstantSet { get; init; }

    internal ExecutionInCheck(
        ExecutionExpression expression,
        IReadOnlyList<ExecutionExpression> values,
        Type returnType,
        ExecutionConstantInSet? constantSet = null)
        : this(expression, values, ExecutionClrBindingFactory.FromClr(returnType), constantSet)
    {
    }
}
