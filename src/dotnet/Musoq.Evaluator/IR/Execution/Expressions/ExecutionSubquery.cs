using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSubquery : ExecutionExpression
{
    public ExecutionSubquery(string identifier, SubqueryResultKind kind, ExecutionPlan plan,
        IReadOnlyList<ExecutionExpression> bindings, ExecutionExpression? operand, ExecutionTypeRef returnType)
        : base(returnType)
    {
        Identifier = identifier;
        Kind = kind;
        Plan = plan;
        Bindings = ExecutionIrCollections.Freeze(bindings);
        Operand = operand;
    }

    public BinaryOpKind Comparison { get; init; } = BinaryOpKind.Equal;
    public string Identifier { get; init; }
    public SubqueryResultKind Kind { get; init; }
    public ExecutionPlan Plan { get; init; }
    private IReadOnlyList<ExecutionExpression> _bindings = [];
    public IReadOnlyList<ExecutionExpression> Bindings
    {
        get => _bindings;
        init => _bindings = ExecutionIrCollections.Freeze(value);
    }
    public ExecutionExpression? Operand { get; init; }
    private IReadOnlyList<ExecutionExpression> _capturedScriptValues = [];
    public IReadOnlyList<ExecutionExpression> CapturedScriptValues
    {
        get => _capturedScriptValues;
        init => _capturedScriptValues = ExecutionIrCollections.Freeze(value);
    }
    private IReadOnlyList<ExecutionExpression> _capturedCteValues = [];
    public IReadOnlyList<ExecutionExpression> CapturedCteValues
    {
        get => _capturedCteValues;
        init => _capturedCteValues = ExecutionIrCollections.Freeze(value);
    }
    internal IReadOnlyList<SubqueryCteBinding> CteBindings { get; init; } = [];
}
