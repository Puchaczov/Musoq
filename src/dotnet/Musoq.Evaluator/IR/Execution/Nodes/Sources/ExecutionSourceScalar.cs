using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Portable source-computation metadata; these nodes are evaluated by the provider.</summary>
public abstract record ExecutionSourceScalar(ExecutionTypeRef ResultType);
public sealed record ExecutionSourceLiteral(ExecutionConstantValue Value, ExecutionTypeRef Type) : ExecutionSourceScalar(Type);
public sealed record ExecutionSourceColumn(string Name, ExecutionTypeRef Type) : ExecutionSourceScalar(Type);
public sealed record ExecutionSourceUnary(SourceScalarUnaryOperator Operator, ExecutionSourceScalar Operand,
    ExecutionTypeRef Type) : ExecutionSourceScalar(Type);
public sealed record ExecutionSourceBinary(SourceScalarBinaryOperator Operator, ExecutionSourceScalar Left,
    ExecutionSourceScalar Right, ExecutionTypeRef Type) : ExecutionSourceScalar(Type)
{
    public bool UsesSqlNullSemantics { get; init; }
}
public sealed record ExecutionSourceCast(ExecutionSourceScalar Operand, ExecutionTypeRef Type) : ExecutionSourceScalar(Type);
public sealed record ExecutionSourceNullCheck(ExecutionSourceScalar Operand, bool Negated,
    ExecutionTypeRef Type) : ExecutionSourceScalar(Type);
public sealed record ExecutionSourceCoalesce : ExecutionSourceScalar
{
    public ExecutionSourceCoalesce(IEnumerable<ExecutionSourceScalar> expressions, ExecutionTypeRef type) : base(type)
        => Expressions = Array.AsReadOnly(expressions.ToArray());

    public IReadOnlyList<ExecutionSourceScalar> Expressions { get; }
}

public sealed record ExecutionSourceComputedProjection(string Name, ExecutionSourceScalar Expression,
    ExecutionTypeRef ResultType, string ExpressionIdentity)
{
    internal static ExecutionSourceComputedProjection FromSchema(SourceComputedProjection projection)
        => new(projection.Name, FromScalar(projection.Expression), ExecutionClrBindingFactory.FromClr(projection.ResultType),
            SourceScalarExpressionFingerprint.Compute(projection.Expression));

    internal SourceComputedProjection ToSchema()
    {
        ValidateGraph(Expression);
        var expression = ToScalar(Expression);
        var projection = new SourceComputedProjection(Name, expression, ResultType.ResolveClrType());
        if (!SourceComputedProjectionFacts.CanProviderEvaluate(projection, SourceComputedProjectionCapabilities.AllPortable) ||
            ExpressionIdentity != SourceScalarExpressionFingerprint.Compute(expression))
            throw new InvalidOperationException("Portable computed projection has inconsistent type or expression identity.");
        return projection;
    }

    internal static IEnumerable<ExecutionSourceScalar> Nodes(ExecutionSourceScalar expression)
    {
        yield return expression;
        IEnumerable<ExecutionSourceScalar> children = expression switch
        {
            ExecutionSourceUnary unary => [unary.Operand],
            ExecutionSourceBinary binary => [binary.Left, binary.Right],
            ExecutionSourceCast cast => [cast.Operand],
            ExecutionSourceNullCheck check => [check.Operand],
            ExecutionSourceCoalesce coalesce => coalesce.Expressions,
            _ => []
        };
        foreach (var child in children)
            foreach (var descendant in Nodes(child))
                yield return descendant;
    }

    private static void ValidateGraph(ExecutionSourceScalar root)
    {
        var pending = new Stack<(ExecutionSourceScalar Node, int Depth)>();
        pending.Push((root, 0));
        var count = 0;
        while (pending.TryPop(out var frame))
        {
            if (++count > 256 || frame.Depth > 32 || frame.Node is not { ResultType: not null } node)
                throw new InvalidOperationException("Invalid portable source scalar graph.");
            var declaredType = node switch
            {
                ExecutionSourceLiteral literal when literal.Value is not null && literal.Value.Kind != ExecutionConstantKind.ClrOnly => literal.Type,
                ExecutionSourceColumn column => column.Type,
                ExecutionSourceUnary unary => unary.Type,
                ExecutionSourceBinary binary => binary.Type,
                ExecutionSourceCast cast => cast.Type,
                ExecutionSourceNullCheck check => check.Type,
                ExecutionSourceCoalesce => node.ResultType,
                _ => null
            };
            if (declaredType?.StableId != node.ResultType.StableId)
                throw new InvalidOperationException("Portable source scalar has inconsistent type metadata.");
            IEnumerable<ExecutionSourceScalar> children = node switch
            {
                ExecutionSourceUnary unary => [unary.Operand],
                ExecutionSourceBinary binary => [binary.Left, binary.Right],
                ExecutionSourceCast cast => [cast.Operand],
                ExecutionSourceNullCheck check => [check.Operand],
                ExecutionSourceCoalesce coalesce => coalesce.Expressions,
                _ => []
            };
            foreach (var child in children)
                pending.Push((child, frame.Depth + 1));
        }
    }

    private static ExecutionSourceScalar FromScalar(SourceScalarExpression expression)
    {
        var type = ExecutionClrBindingFactory.FromClr(expression.ResultType);
        return expression switch
        {
            SourceScalarLiteral literal => new ExecutionSourceLiteral(ExecutionConstantValue.FromClr(literal.Value, type), type),
            SourceScalarColumn column => new ExecutionSourceColumn(column.Column.Name, type),
            SourceScalarUnary unary => new ExecutionSourceUnary(unary.Operator, FromScalar(unary.Operand), type),
            SourceScalarBinary binary => new ExecutionSourceBinary(binary.Operator, FromScalar(binary.Left), FromScalar(binary.Right), type) { UsesSqlNullSemantics = binary.UsesSqlNullSemantics },
            SourceScalarCast cast => new ExecutionSourceCast(FromScalar(cast.Operand), type),
            SourceScalarNullCheck check => new ExecutionSourceNullCheck(FromScalar(check.Operand), check.Negated, type),
            SourceScalarCoalesce coalesce => new ExecutionSourceCoalesce(coalesce.Expressions.Select(FromScalar), type),
            _ => throw new InvalidOperationException("Unknown source scalar metadata.")
        };
    }

    private static SourceScalarExpression ToScalar(ExecutionSourceScalar expression)
    {
        var type = expression.ResultType.ResolveClrType();
        return expression switch
        {
            ExecutionSourceLiteral literal => new SourceScalarLiteral(literal.Value.ToClrValue(), type),
            ExecutionSourceColumn column => new SourceScalarColumn(column.Name, type),
            ExecutionSourceUnary unary => new SourceScalarUnary(unary.Operator, ToScalar(unary.Operand), type),
            ExecutionSourceBinary binary => new SourceScalarBinary(binary.Operator, ToScalar(binary.Left), ToScalar(binary.Right), type) { UsesSqlNullSemantics = binary.UsesSqlNullSemantics },
            ExecutionSourceCast cast => new SourceScalarCast(ToScalar(cast.Operand), type),
            ExecutionSourceNullCheck check when type == typeof(bool) => new SourceScalarNullCheck(ToScalar(check.Operand), check.Negated),
            ExecutionSourceCoalesce coalesce => new SourceScalarCoalesce(coalesce.Expressions.Select(ToScalar), type),
            _ => throw new InvalidOperationException("Unknown or incorrectly typed portable source scalar metadata.")
        };
    }
}
