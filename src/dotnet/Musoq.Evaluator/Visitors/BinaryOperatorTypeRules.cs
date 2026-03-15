using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal enum BinaryOperatorKind
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,
    LeftShift,
    RightShift,
    Equality,
    Inequality,
    Relational
}

internal static class BinaryOperatorTypeRules
{
    internal static Type NormalizeOperandType(Type type)
    {
        if (type is NullNode.NullType)
            return type;

        return BuildMetadataAndInferTypesVisitorUtilities.StripNullable(type);
    }

    internal static bool CanSkipStaticTypeValidation(Type type)
    {
        return type == typeof(object) ||
               type is NullNode.NullType ||
               !BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(type);
    }

    internal static bool IsIntegralType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
    }

    internal static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    internal static Type GetWiderNumericType(Type left, Type right)
    {
        var typeOrder = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double)
        };

        var leftIndex = Array.IndexOf(typeOrder, left);
        var rightIndex = Array.IndexOf(typeOrder, right);

        if (leftIndex < 0 && rightIndex < 0) return typeof(int);
        if (leftIndex < 0) return right;
        if (rightIndex < 0) return left;

        return leftIndex > rightIndex ? left : right;
    }

    internal static bool CanApplyAddition(Type leftType, Type rightType)
    {
        if (leftType == typeof(string) && rightType == typeof(string))
            return true;

        if (IsNumericType(leftType) && IsNumericType(rightType))
            return true;

        if (leftType == typeof(DateTime) && rightType == typeof(TimeSpan))
            return true;

        if (leftType == typeof(TimeSpan) && rightType == typeof(DateTime))
            return true;

        if (leftType == typeof(DateTimeOffset) && rightType == typeof(TimeSpan))
            return true;

        if (leftType == typeof(TimeSpan) && rightType == typeof(DateTimeOffset))
            return true;

        return leftType == typeof(TimeSpan) && rightType == typeof(TimeSpan);
    }

    internal static bool CanApplySubtraction(Type leftType, Type rightType)
    {
        if (IsNumericType(leftType) && IsNumericType(rightType))
            return true;

        if (leftType == typeof(DateTime) && (rightType == typeof(DateTime) || rightType == typeof(TimeSpan)))
            return true;

        if (leftType == typeof(DateTimeOffset) &&
            (rightType == typeof(DateTimeOffset) || rightType == typeof(TimeSpan)))
            return true;

        return leftType == typeof(TimeSpan) && rightType == typeof(TimeSpan);
    }

    internal static bool CanApplyNumericOperator(Type leftType, Type rightType)
    {
        return IsNumericType(leftType) && IsNumericType(rightType);
    }

    internal static bool CanApplyBitwiseOperator(Type leftType, Type rightType)
    {
        return IsIntegralType(leftType) && IsIntegralType(rightType);
    }

    internal static bool CanApplyShiftOperator(Type leftType, Type rightType)
    {
        return IsIntegralType(leftType) && IsIntegralType(rightType);
    }

    internal static bool CanApplyEqualityOperator(Type leftType, Type rightType)
    {
        if (leftType == rightType)
            return true;

        if ((leftType == typeof(char) && rightType == typeof(string)) ||
            (leftType == typeof(string) && rightType == typeof(char)))
            return true;

        return IsNumericType(leftType) && IsNumericType(rightType);
    }

    internal static bool CanApplyRelationalOperator(Type leftType, Type rightType)
    {
        if (IsNumericType(leftType) && IsNumericType(rightType))
            return true;

        if (leftType != rightType)
            return false;

        return leftType == typeof(string) || leftType == typeof(DateTime) || leftType == typeof(DateTimeOffset) ||
               leftType == typeof(TimeSpan);
    }
}
