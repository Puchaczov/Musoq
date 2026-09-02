using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>
/// Target-neutral scalar expression that a datasource may evaluate as a
/// computed projection when its advertised capabilities are sufficient.
/// </summary>
public abstract record SourceScalarExpression(Type ResultType, ColumnStability Stability = ColumnStability.Stable)
{
    protected static Type RequireType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type;
    }
}

/// <summary>Portable unary operators understood by source projection negotiation.</summary>
public enum SourceScalarUnaryOperator
{
    Negate,
    Positive,
    Not,
    BitwiseNot
}

/// <summary>Portable binary operators understood by source projection negotiation.</summary>
public enum SourceScalarBinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    And,
    Or
}

/// <summary>A literal value in a portable source scalar expression.</summary>
public sealed record SourceScalarLiteral(object? Value, Type ValueType)
    : SourceScalarExpression(RequireType(ValueType));

/// <summary>A source column read with an explicit stability contract.</summary>
public sealed record SourceScalarColumn(SourceColumnRef Column, Type ValueType, ColumnStability ColumnStability = ColumnStability.Stable)
    : SourceScalarExpression(RequireType(ValueType), ColumnStability)
{
    public SourceScalarColumn(string columnName, Type valueType, ColumnStability stability = ColumnStability.Stable)
        : this(new SourceColumnRef(columnName), valueType, stability)
    {
    }
}

/// <summary>A unary operation over a portable source scalar.</summary>
public sealed record SourceScalarUnary(
    SourceScalarUnaryOperator Operator,
    SourceScalarExpression Operand,
    Type ValueType,
    ColumnStability ExpressionStability = ColumnStability.Stable)
    : SourceScalarExpression(RequireType(ValueType), ExpressionStability);

/// <summary>A binary operation over portable source scalars.</summary>
public sealed record SourceScalarBinary(
    SourceScalarBinaryOperator Operator,
    SourceScalarExpression Left,
    SourceScalarExpression Right,
    Type ValueType,
    ColumnStability ExpressionStability = ColumnStability.Stable)
    : SourceScalarExpression(RequireType(ValueType), ExpressionStability);

/// <summary>A checked portable cast over a source scalar.</summary>
public sealed record SourceScalarCast(
    SourceScalarExpression Operand,
    Type ValueType,
    ColumnStability ExpressionStability = ColumnStability.Stable)
    : SourceScalarExpression(RequireType(ValueType), ExpressionStability);

/// <summary>A SQL null-presence check over a source scalar.</summary>
public sealed record SourceScalarNullCheck(
    SourceScalarExpression Operand,
    bool Negated = false,
    ColumnStability ExpressionStability = ColumnStability.Stable)
    : SourceScalarExpression(typeof(bool), ExpressionStability);

/// <summary>A SQL coalesce expression over portable source scalars.</summary>
public sealed record SourceScalarCoalesce(
    IReadOnlyList<SourceScalarExpression> Expressions,
    Type ValueType,
    ColumnStability ExpressionStability = ColumnStability.Stable)
    : SourceScalarExpression(RequireType(ValueType), ExpressionStability)
{
    public SourceScalarCoalesce(IEnumerable<SourceScalarExpression> expressions, Type valueType)
        : this(expressions.ToArray(), valueType)
    {
    }
}

/// <summary>Shared stability facts for portable source scalar expressions.</summary>
public static class SourceScalarExpressionFacts
{
    public static bool IsStable(SourceScalarExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (expression.Stability != ColumnStability.Stable)
            return false;

        return expression switch
        {
            SourceScalarColumn column => column.ColumnStability == ColumnStability.Stable,
            SourceScalarUnary unary => unary.ExpressionStability == ColumnStability.Stable && IsStable(unary.Operand),
            SourceScalarBinary binary => binary.ExpressionStability == ColumnStability.Stable && IsStable(binary.Left) && IsStable(binary.Right),
            SourceScalarCast cast => cast.ExpressionStability == ColumnStability.Stable && IsStable(cast.Operand),
            SourceScalarNullCheck nullCheck => nullCheck.ExpressionStability == ColumnStability.Stable && IsStable(nullCheck.Operand),
            SourceScalarCoalesce coalesce => coalesce.ExpressionStability == ColumnStability.Stable && coalesce.Expressions.All(IsStable),
            _ => true
        };
    }
}

/// <summary>Produces deterministic semantic identities for source scalar expressions.</summary>
public static class SourceScalarExpressionFingerprint
{
    public static string Compute(SourceScalarExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return expression switch
        {
            SourceScalarLiteral literal => $"literal:{literal.Value?.GetType().AssemblyQualifiedName}:{literal.Value}",
            SourceScalarColumn column => $"column:{column.Column.Name}:{column.ResultType.AssemblyQualifiedName}:{column.ColumnStability}",
            SourceScalarUnary unary => $"unary:{unary.Operator}:{Compute(unary.Operand)}:{unary.ResultType.AssemblyQualifiedName}:{unary.ExpressionStability}",
            SourceScalarBinary binary => $"binary:{binary.Operator}:{Compute(binary.Left)}:{Compute(binary.Right)}:{binary.ResultType.AssemblyQualifiedName}:{binary.ExpressionStability}",
            SourceScalarCast cast => $"cast:{Compute(cast.Operand)}:{cast.ResultType.AssemblyQualifiedName}:{cast.ExpressionStability}",
            SourceScalarNullCheck nullCheck => $"null:{nullCheck.Negated}:{Compute(nullCheck.Operand)}:{nullCheck.ExpressionStability}",
            SourceScalarCoalesce coalesce => $"coalesce:{string.Join(",", coalesce.Expressions.Select(Compute))}:{coalesce.ResultType.AssemblyQualifiedName}:{coalesce.ExpressionStability}",
            _ => expression.ToString() ?? expression.GetType().FullName ?? expression.GetType().Name
        };
    }
}
