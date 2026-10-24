using Musoq.Evaluator.IR.Logical;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Planning.Subqueries;

namespace Musoq.Evaluator.IR.Expressions;

public enum SubqueryResultKind
{
    Scalar,
    Exists,
    In,
    Any,
    All
}

public sealed record SubqueryExpression(
    string Identifier,
    SubqueryResultKind Kind,
    LogicalNode Query,
    IrExpression[] Bindings,
    IrExpression? Operand,
    Type ReturnType) : IrExpression(ReturnType)
{
    public BinaryOpKind Comparison { get; init; } = BinaryOpKind.Equal;
    public IEnumerable<IrExpression> Inputs => Operand is null ? Bindings : Bindings.Append(Operand);
    internal SubqueryPlanningArtifacts? Planning { get; init; }
}

public sealed record SubqueryParameterRef(int Index, Type ReturnType) : IrExpression(ReturnType);
