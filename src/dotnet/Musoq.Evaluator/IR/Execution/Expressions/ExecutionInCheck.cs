using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInCheck : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _values = [];

    public ExecutionInCheck(
        ExecutionExpression expression,
        IReadOnlyList<ExecutionExpression> values,
        ExecutionTypeRef returnType,
        ExecutionConstantInSet? constantSet = null,
        bool isNegated = false)
        : base(returnType)
    {
        Expression = expression;
        Values = ExecutionIrCollections.Freeze(values);
        ReturnType = returnType;
        ConstantSet = constantSet;
        IsNegated = isNegated;
    }

    public ExecutionExpression Expression { get; init; }

    public IReadOnlyList<ExecutionExpression> Values
    {
        get => _values;
        init => _values = ExecutionIrCollections.Freeze(value);
    }

    public override ExecutionTypeRef ReturnType { get; init; }

    public ExecutionConstantInSet? ConstantSet { get; init; }

    public bool IsNegated { get; init; }

    internal ExecutionInCheck(
        ExecutionExpression expression,
        IReadOnlyList<ExecutionExpression> values,
        Type returnType,
        ExecutionConstantInSet? constantSet = null,
        bool isNegated = false)
        : this(expression, values, ExecutionClrBindingFactory.FromClr(returnType), constantSet, isNegated)
    {
    }
}
