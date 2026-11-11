using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

internal static class SourceScalarExpressionGraph
{
    internal static bool IsValid(SourceScalarExpression? expression)
    {
        var pending = new Stack<(SourceScalarExpression? Expression, int Depth)>();
        pending.Push((expression, 0));
        var count = 0;
        while (pending.TryPop(out var item))
        {
            if (++count > 256 || item.Depth > 32 || item.Expression is not { ResultType: not null } current ||
                current.ResultType == typeof(object) || !QueryRowField.IsSupportedFieldType(current.ResultType))
                return false;
            if (!HasConsistentType(current))
                return false;
            switch (current)
            {
                case SourceScalarLiteral literal when IsLiteral(literal):
                    break;
                case SourceScalarColumn { Column.Name: { Length: > 0 } }:
                    break;
                case SourceScalarUnary unary when (uint)unary.Operator <= (uint)SourceScalarUnaryOperator.BitwiseNot:
                    pending.Push((unary.Operand, item.Depth + 1));
                    break;
                case SourceScalarBinary binary when (uint)binary.Operator <= (uint)SourceScalarBinaryOperator.Or:
                    pending.Push((binary.Left, item.Depth + 1));
                    pending.Push((binary.Right, item.Depth + 1));
                    break;
                case SourceScalarCast cast:
                    pending.Push((cast.Operand, item.Depth + 1));
                    break;
                case SourceScalarNullCheck check:
                    pending.Push((check.Operand, item.Depth + 1));
                    break;
                case SourceScalarCoalesce { Expressions.Count: >= 2 } coalesce:
                    foreach (var child in coalesce.Expressions)
                        pending.Push((child, item.Depth + 1));
                    break;
                default:
                    return false;
            }
        }
        return true;
    }

    private static bool HasConsistentType(SourceScalarExpression expression) => expression.ResultType == (expression switch
    {
        SourceScalarLiteral literal => literal.ValueType,
        SourceScalarColumn column => column.ValueType,
        SourceScalarUnary unary => unary.ValueType,
        SourceScalarBinary binary => binary.ValueType,
        SourceScalarCast cast => cast.ValueType,
        SourceScalarNullCheck => typeof(bool),
        SourceScalarCoalesce coalesce => coalesce.ValueType,
        _ => null
    });

    private static bool IsLiteral(SourceScalarLiteral literal)
    {
        var type = Nullable.GetUnderlyingType(literal.ResultType) ?? literal.ResultType;
        return literal.Value switch
        {
            null => !literal.ResultType.IsValueType || Nullable.GetUnderlyingType(literal.ResultType) is not null,
            bool => type == typeof(bool), byte => type == typeof(byte), sbyte => type == typeof(sbyte),
            short => type == typeof(short), ushort => type == typeof(ushort), int => type == typeof(int), uint => type == typeof(uint),
            long => type == typeof(long), ulong => type == typeof(ulong), float => type == typeof(float), double => type == typeof(double),
            decimal => type == typeof(decimal), char => type == typeof(char), string => type == typeof(string),
            DateTime => type == typeof(DateTime), DateTimeOffset => type == typeof(DateTimeOffset), TimeSpan => type == typeof(TimeSpan),
            Guid => type == typeof(Guid), _ => false
        };
    }
}
