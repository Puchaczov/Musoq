using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCaseWhen : ExecutionExpression
{
    private IReadOnlyList<ExecutionCaseWhenBranch> _branches = [];

    public ExecutionCaseWhen(
        IReadOnlyList<ExecutionCaseWhenBranch> branches,
        ExecutionExpression? elseExpression,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Branches = ExecutionIrCollections.Freeze(branches);
        ElseExpression = elseExpression;
        ReturnType = returnType;
    }

    public IReadOnlyList<ExecutionCaseWhenBranch> Branches
    {
        get => _branches;
        init => _branches = ExecutionIrCollections.Freeze(value);
    }

    public ExecutionExpression? ElseExpression { get; init; }

    public override ExecutionTypeRef ReturnType { get; init; }

    internal ExecutionCaseWhen(
        IReadOnlyList<ExecutionCaseWhenBranch> branches,
        ExecutionExpression? elseExpression,
        Type returnType)
        : this(branches, elseExpression, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
