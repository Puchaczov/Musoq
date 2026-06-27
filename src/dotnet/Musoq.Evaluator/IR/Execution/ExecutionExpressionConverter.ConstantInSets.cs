using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionConstantInSet? TryCreateConstantInSet(InCheck inCheck)
    {
        if (inCheck.Values.Count == 0)
            return null;

        var values = new object?[inCheck.Values.Count];
        for (var index = 0; index < inCheck.Values.Count; index++)
        {
            if (inCheck.Values[index] is not Literal literal || !CanHoistLiteral(literal.Value))
                return null;

            values[index] = literal.Value;
        }

        var elementType = inCheck.Expression.ReturnType;
        if (!CanUseConstantInElementType(elementType, values))
            return null;

        var kind = SelectConstantInSetKind(elementType, values);

        return new ExecutionConstantInSet(elementType, values, kind);
    }

    private static ExecutionConstantInSetKind SelectConstantInSetKind(
        Type elementType,
        object?[] values)
    {
        if (CanUseSwitchInSet(elementType, values))
            return ExecutionConstantInSetKind.Switch;

        var threshold = GetArrayInValueThreshold(elementType);
        return values.Length <= threshold
            ? ExecutionConstantInSetKind.Array
            : ExecutionConstantInSetKind.FrozenSet;
    }

    private static bool CanUseSwitchInSet(Type elementType, object?[] values)
    {
        if (values.Length <= DefaultArrayInValueThreshold || values.Length > PrimitiveArrayInValueThreshold)
            return false;

        var valueType = Nullable.GetUnderlyingType(elementType) ?? elementType;
        if (valueType != typeof(string) && valueType != typeof(bool) && valueType != typeof(char))
            return false;

        return values.All(value => value == null || value.GetType() == valueType);
    }

    private static int GetArrayInValueThreshold(Type elementType)
    {
        var valueType = Nullable.GetUnderlyingType(elementType) ?? elementType;
        return UsesPrimitiveArrayStrategy(valueType)
            ? PrimitiveArrayInValueThreshold
            : DefaultArrayInValueThreshold;
    }

    private static bool UsesPrimitiveArrayStrategy(Type valueType)
    {
        return valueType.IsEnum ||
               Type.GetTypeCode(valueType) is TypeCode.Boolean or
                   TypeCode.Byte or
                   TypeCode.Char or
                   TypeCode.DateTime or
                   TypeCode.Decimal or
                   TypeCode.Double or
                   TypeCode.Int16 or
                   TypeCode.Int32 or
                   TypeCode.Int64 or
                   TypeCode.SByte or
                   TypeCode.Single or
                   TypeCode.String or
                   TypeCode.UInt16 or
                   TypeCode.UInt32 or
                   TypeCode.UInt64;
    }

    private static bool CanUseConstantInElementType(Type elementType, IReadOnlyList<object?> values)
    {
        if (elementType.IsByRef || elementType.IsPointer || elementType == typeof(void))
            return false;

        if (!elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null)
            return true;

        return values.All(static value => value != null);
    }

    private static bool CanHoistLiteral(object? value)
    {
        return value is null or string or bool or char or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal;
    }
}
