using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static class SourceScalarProjectionConverter
{
    public static SourceScalarExpression? Convert(
        IrExpression expression,
        SchemaScanNode scan,
        SourceDescriptor descriptor
    ) =>
        Musoq.Evaluator.IR.SourcePlanning.SourcePredicateGraphValidator.IsWithinBudget(
            expression,
            System.Threading.CancellationToken.None
        )
            ? Convert(expression, scan, descriptor, 0)
            : null;

    private static SourceScalarExpression? Convert(
        IrExpression expression,
        SchemaScanNode scan,
        SourceDescriptor descriptor,
        int depth
    )
    {
        if (depth > 24 || expression.EnumType != null || expression.ReturnType == typeof(object))
            return null;
        SourceScalarExpression? Child(IrExpression child) => Convert(child, scan, descriptor, depth + 1);
        return expression switch
        {
            Literal literal => new SourceScalarLiteral(literal.Value, literal.ReturnType),
            ColumnRef column
                when column.Alias.Equals(scan.Alias, StringComparison.OrdinalIgnoreCase)
                    && descriptor.Columns.Any(item =>
                        item.ColumnName == column.ColumnName
                        && item.ColumnType == column.ReturnType
                        && item.Stability == ColumnStability.Stable
                        && item.ReadModifiers.Count == 0
                        && string.IsNullOrWhiteSpace(item.IntendedTypeName)
                        && item.SourceReadType == item.ColumnType
                    ) => new SourceScalarColumn(column.ColumnName, column.ReturnType),
            StrictCast cast when Child(cast.Expression) is { } operand => new SourceScalarCast(
                operand,
                cast.ReturnType
            ),
            UnaryOp unary
                when unary.Kind is UnaryOpKind.Not or UnaryOpKind.Negate && Child(unary.Operand) is { } operand =>
                new SourceScalarUnary(
                    unary.Kind == UnaryOpKind.Not ? SourceScalarUnaryOperator.Not : SourceScalarUnaryOperator.Negate,
                    operand,
                    unary.ReturnType
                ),
            BinaryOp binary
                when Operator(binary.Kind) is { } operation
                    && Child(binary.Left) is { } left
                    && Child(binary.Right) is { } right => new SourceScalarBinary(
                operation,
                left,
                right,
                binary.ReturnType
            )
            {
                UsesSqlNullSemantics = binary.UsesSqlNullSemantics,
            },
            IsNullCheck check when Child(check.Expression) is { } operand && check.ReturnType == typeof(bool) =>
                new SourceScalarNullCheck(operand, check.IsNegated),
            Coalesce coalesce => Coalesce(coalesce, Child),
            _ => null,
        };
    }

    private static SourceScalarExpression? Coalesce(
        Coalesce expression,
        Func<IrExpression, SourceScalarExpression?> convert
    )
    {
        var operands = expression.Expressions.Select(convert).ToArray();
        return operands.Length >= 2 && operands.All(item => item is not null)
            ? new SourceScalarCoalesce(operands.Select(item => item!).ToArray(), expression.ReturnType)
            : null;
    }

    private static SourceScalarBinaryOperator? Operator(BinaryOpKind kind) =>
        kind switch
        {
            BinaryOpKind.Add => SourceScalarBinaryOperator.Add,
            BinaryOpKind.Subtract => SourceScalarBinaryOperator.Subtract,
            BinaryOpKind.Multiply => SourceScalarBinaryOperator.Multiply,
            BinaryOpKind.Divide => SourceScalarBinaryOperator.Divide,
            BinaryOpKind.Modulo => SourceScalarBinaryOperator.Modulo,
            BinaryOpKind.Equal => SourceScalarBinaryOperator.Equal,
            BinaryOpKind.NotEqual => SourceScalarBinaryOperator.NotEqual,
            BinaryOpKind.LessThan => SourceScalarBinaryOperator.LessThan,
            BinaryOpKind.LessOrEqual => SourceScalarBinaryOperator.LessThanOrEqual,
            BinaryOpKind.GreaterThan => SourceScalarBinaryOperator.GreaterThan,
            BinaryOpKind.GreaterOrEqual => SourceScalarBinaryOperator.GreaterThanOrEqual,
            BinaryOpKind.And => SourceScalarBinaryOperator.And,
            BinaryOpKind.Or => SourceScalarBinaryOperator.Or,
            _ => null,
        };
}
