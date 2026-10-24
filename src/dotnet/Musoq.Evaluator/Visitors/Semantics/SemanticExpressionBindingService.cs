using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Schema.Managers;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;
using static Musoq.Evaluator.Visitors.SemanticExpressionDiagnosticFacts;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticExpressionBindingService(SemanticDiagnosticReporter diagnosticReporter)
{
    public Node BindCoalesce(CoalesceNode original, Node left, Node right)
    {
        if (IsNullLiteral(left))
            return FoldNullLeftOperand(right);

        ValidateCoalesceFallback(left, right, original);
        var returnType = ResolveCoalesceReturnType(left, right);
        var convertedLeft = ConvertCoalesceOperand(left, returnType);
        if (IsStaticallyNonNullableValueType(left.ReturnType))
            return convertedLeft;

        var result = new CoalesceNode(convertedLeft, ConvertCoalesceOperand(RetypeNullFallback(right, left), returnType), returnType);
        if (original.HasSpan)
            result.WithSpan(original.Span);

        return result;
    }

    private static Node ConvertCoalesceOperand(Node operand, Type resultType)
    {
        var sourceType = operand.ReturnType;
        if (sourceType is null || IsNullLiteral(operand))
            return operand;
        var sourceValue = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var resultValue = Nullable.GetUnderlyingType(resultType) ?? resultType;
        if (sourceValue == resultValue || CallableOverloadPreference.FindCommonNumericType(sourceValue, resultValue) != resultValue)
            return operand;

        var targetType = Nullable.GetUnderlyingType(sourceType) is null ? resultValue : typeof(Nullable<>).MakeGenericType(resultValue);
        return new CastNode(operand, resultValue.Name, targetType).CopySpansFrom(operand);
    }

    private void ValidateCoalesceFallback(Node left, Node right, Node errorContextNode)
    {
        if (AreCoalesceTypesCompatible(left.ReturnType, right.ReturnType))
            return;

        var message = $"Operator ?? requires compatible fallback types, but got '{FormatTypeName(left.ReturnType)}' and '{FormatTypeName(right.ReturnType)}'.";
        if (diagnosticReporter.TryReportTypeMismatch(message, errorContextNode))
            return;

        throw new TypeMismatchException(message);
    }

    private static Node FoldNullLeftOperand(Node right)
    {
        return IsNullLiteral(right)
            ? new NullNode(typeof(object), right.Span)
            : right;
    }

    private static Node RetypeNullFallback(Node right, Node left)
    {
        if (!IsNullLiteral(right))
            return right;

        return new NullNode(ResolveCoalesceReturnType(left, right), right.Span);
    }

    private static bool AreCoalesceTypesCompatible(Type? leftType, Type? rightType)
    {
        if (leftType == null || rightType == null)
            return true;

        if (IsNullNodeType(leftType) || IsNullNodeType(rightType))
            return true;

        var leftValueType = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var rightValueType = Nullable.GetUnderlyingType(rightType) ?? rightType;

        if (CanSkipStaticTypeValidation(leftValueType) || CanSkipStaticTypeValidation(rightValueType))
            return true;

        if (leftValueType == rightValueType)
            return true;

        if (CallableOverloadPreference.FindCommonNumericType(leftValueType, rightValueType) is not null)
            return true;

        if (leftValueType.IsValueType || rightValueType.IsValueType)
            return false;

        return leftValueType.IsAssignableFrom(rightValueType) || rightValueType.IsAssignableFrom(leftValueType);
    }

    private static Type ResolveCoalesceReturnType(Node left, Node right)
    {
        var leftType = left.ReturnType;
        var rightType = right.ReturnType;

        if (leftType == null || IsNullNodeType(leftType))
            return ResolveNullLeftReturnType(rightType);

        if (rightType == null || IsNullNodeType(rightType))
            return leftType;

        var leftUnderlyingType = Nullable.GetUnderlyingType(leftType);
        var numericType = CallableOverloadPreference.FindCommonNumericType(leftUnderlyingType ?? leftType,
            Nullable.GetUnderlyingType(rightType) ?? rightType);
        if (numericType is not null)
            return leftUnderlyingType is not null && Nullable.GetUnderlyingType(rightType) is not null
                ? typeof(Nullable<>).MakeGenericType(numericType)
                : numericType;

        if (leftUnderlyingType != null)
            return Nullable.GetUnderlyingType(rightType) == null ? leftUnderlyingType : leftType;

        if (leftType == typeof(object) || rightType == typeof(object))
            return typeof(object);

        if (!leftType.IsValueType && !rightType.IsValueType)
            return ResolveReferenceCoalesceReturnType(leftType, rightType);

        return leftType;
    }

    private static Type ResolveNullLeftReturnType(Type? rightType)
    {
        if (rightType == null || IsNullNodeType(rightType))
            return typeof(object);

        return rightType;
    }

    private static Type ResolveReferenceCoalesceReturnType(Type leftType, Type rightType)
    {
        if (leftType.IsAssignableFrom(rightType))
            return leftType;

        return rightType.IsAssignableFrom(leftType) ? rightType : typeof(object);
    }

    private static bool IsStaticallyNonNullableValueType(Type? type)
    {
        return type is { IsValueType: true } && Nullable.GetUnderlyingType(type) == null && !IsNullNodeType(type);
    }

    private static bool IsNullLiteral(Node node)
    {
        return node is NullNode || IsNullNodeType(node.ReturnType);
    }

    private static bool IsNullNodeType(Type? type)
    {
        return type is NullNode.NullType;
    }
}
