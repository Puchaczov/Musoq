using System;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionMembershipBindingResolver
{
    internal static ExecutionMembershipBinding Bind(InCheck membership)
        => Bind(membership.Expression.ReturnType,
            membership.Values.Select(value => value.ReturnType).ToArray(), membership.SqlSemantics);

    internal static ExecutionMembershipBinding Bind(ExecutionInCheck membership)
        => Bind(membership.Expression.ReturnType.ResolveClrType(),
            membership.Values.Select(value => value.ReturnType.ResolveClrType()).ToArray(), membership.UsesSqlNullSemantics);

    private static ExecutionMembershipBinding Bind(Type operandType, Type[] memberTypes, bool sqlSemantics)
    {
        if (IsNullType(operandType))
            operandType = memberTypes.FirstOrDefault(type => !IsNullType(type)) ?? typeof(bool?);
        operandType = NullableCarrier(operandType);
        var members = memberTypes.Select(type =>
            IsNullType(type) ? operandType : NullableCarrier(type)).ToArray();
        var comparisons = members.Select(type => BindComparison(operandType, type, sqlSemantics)).ToArray();
        return new ExecutionMembershipBinding(ExecutionClrBindingFactory.FromClr(operandType),
            members.Select(ExecutionClrBindingFactory.FromClr).ToArray(), comparisons,
            sqlSemantics ? ExecutionMembershipEvaluationMode.ShortCircuit : ExecutionMembershipEvaluationMode.Eager);
    }

    private static bool IsNullType(Type type) => type == typeof(NullNode.NullType);

    private static Type NullableCarrier(Type type)
    {
        if (type == typeof(object))
            throw new SourcePlanContractException("Membership requires concrete operand and member types.", TextSpan.Empty);
        if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            return typeof(Nullable<>).MakeGenericType(type);
        return type;
    }

    private static ExecutionMembershipComparison BindComparison(Type left, Type right, bool sqlSemantics)
    {
        var leftValue = Nullable.GetUnderlyingType(left) ?? left;
        var rightValue = Nullable.GetUnderlyingType(right) ?? right;
        if (IsNumeric(leftValue) && IsNumeric(rightValue))
        {
            if (!sqlSemantics && leftValue == rightValue)
                return new(ExecutionMembershipEqualityKind.TypedEquality, ExecutionClrBindingFactory.FromClr(left));
            var numericType = GetNumericComparisonType(leftValue, rightValue);
            return new(ExecutionMembershipEqualityKind.NumericEquality, ExecutionClrBindingFactory.FromClr(numericType));
        }
        if (leftValue == rightValue)
        {
            if (leftValue.IsValueType && !leftValue.IsEnum &&
                !typeof(IEquatable<>).MakeGenericType(leftValue).IsAssignableFrom(leftValue))
                throw new SourcePlanContractException($"Membership type '{leftValue}' requires typed equality.", TextSpan.Empty);
            return new(ExecutionMembershipEqualityKind.TypedEquality, ExecutionClrBindingFactory.FromClr(left));
        }
        return new(ExecutionMembershipEqualityKind.NeverMatch, ExecutionClrBindingFactory.FromClr(left));
    }

    private static Type GetNumericComparisonType(Type left, Type right)
    {
        if (left == typeof(double) || right == typeof(double))
            return typeof(double?);
        if (left == typeof(float) || right == typeof(float))
            return typeof(float?);
        return typeof(decimal?);
    }

    private static bool IsNumeric(Type type)
        => type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
           type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
           type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}
