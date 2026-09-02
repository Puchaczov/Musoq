using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateExpressionConverter
{
    private static bool TryConvertPredicateOperand(
        IrExpression expression,
        string sourceAlias,
        EnumTypeDescriptor? enumType,
        [NotNullWhen(true)] out SourcePredicateExpression? predicate)
    {
        if (enumType != null && expression is Literal literal && literal.Value != null)
        {
            if (!TryCreateEnumScalarValue(enumType.UnderlyingKind, literal.Value, out var value))
            {
                predicate = null;
                return false;
            }

            predicate = new SourcePredicateEnumLiteral(value, enumType.Fingerprint);
            return true;
        }

        return TryConvertPredicate(expression, sourceAlias, out predicate);
    }

    private static bool TryConvertEnumFlagsPredicate(
        MethodCall methodCall,
        string sourceAlias,
        [NotNullWhen(true)] out SourcePredicateExpression? predicate)
    {
        if (methodCall.EnumIntrinsic is not (EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags) ||
            methodCall.OperandEnumType is not { IsFlags: true } descriptor ||
            methodCall.EnumMask is not { } mask ||
            methodCall.Arguments.Count == 0 ||
            !TryConvertPredicate(methodCall.Arguments[0], sourceAlias, out var expression) ||
            expression is not SourcePredicateColumn)
        {
            predicate = null;
            return false;
        }

        predicate = new SourcePredicateFlags(
            expression,
            new SourcePredicateEnumLiteral(mask, descriptor.Fingerprint),
            methodCall.EnumIntrinsic == EnumIntrinsicKind.HasAnyFlags
                ? SourcePredicateFlagsMatchMode.Any
                : SourcePredicateFlagsMatchMode.All);
        return true;
    }

    private static bool TryCreateEnumScalarValue(
        EnumUnderlyingKind kind,
        object value,
        out EnumScalarValue scalar)
    {
        switch (kind)
        {
            case EnumUnderlyingKind.Byte when value is byte typed:
                scalar = EnumScalarValue.FromByte(typed);
                return true;
            case EnumUnderlyingKind.SByte when value is sbyte typed:
                scalar = EnumScalarValue.FromSByte(typed);
                return true;
            case EnumUnderlyingKind.Int16 when value is short typed:
                scalar = EnumScalarValue.FromInt16(typed);
                return true;
            case EnumUnderlyingKind.UInt16 when value is ushort typed:
                scalar = EnumScalarValue.FromUInt16(typed);
                return true;
            case EnumUnderlyingKind.Int32 when value is int typed:
                scalar = EnumScalarValue.FromInt32(typed);
                return true;
            case EnumUnderlyingKind.UInt32 when value is uint typed:
                scalar = EnumScalarValue.FromUInt32(typed);
                return true;
            case EnumUnderlyingKind.Int64 when value is long typed:
                scalar = EnumScalarValue.FromInt64(typed);
                return true;
            case EnumUnderlyingKind.UInt64 when value is ulong typed:
                scalar = EnumScalarValue.FromUInt64(typed);
                return true;
            default:
                scalar = default;
                return false;
        }
    }

    private static bool TryConvertComparisonOperator(
        BinaryOpKind kind,
        out SourcePredicateComparisonOperator comparisonOperator)
    {
        switch (kind)
        {
            case BinaryOpKind.Equal:
                comparisonOperator = SourcePredicateComparisonOperator.Equal;
                return true;
            case BinaryOpKind.NotEqual:
                comparisonOperator = SourcePredicateComparisonOperator.NotEqual;
                return true;
            case BinaryOpKind.GreaterThan:
                comparisonOperator = SourcePredicateComparisonOperator.GreaterThan;
                return true;
            case BinaryOpKind.GreaterOrEqual:
                comparisonOperator = SourcePredicateComparisonOperator.GreaterOrEqual;
                return true;
            case BinaryOpKind.LessThan:
                comparisonOperator = SourcePredicateComparisonOperator.LessThan;
                return true;
            case BinaryOpKind.LessOrEqual:
                comparisonOperator = SourcePredicateComparisonOperator.LessOrEqual;
                return true;
            default:
                comparisonOperator = default;
                return false;
        }
    }
}
