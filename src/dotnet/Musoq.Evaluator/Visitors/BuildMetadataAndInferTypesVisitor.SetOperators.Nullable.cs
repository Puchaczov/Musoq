using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static bool TryReconcileNullSetFields(ref FieldNode left, ref FieldNode right)
    {
        var leftType = left.Expression.ReturnType;
        var rightType = right.Expression.ReturnType;
        var leftIsNull = leftType is NullNode.NullType;
        var rightIsNull = rightType is NullNode.NullType;
        if (!leftIsNull && !rightIsNull)
            return false;
        if (leftIsNull && rightIsNull)
            return true;

        var type = (leftIsNull ? rightType : leftType)!;
        if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            type = typeof(Nullable<>).MakeGenericType(type);
        var intendedType = InterpretationPropertyBinding.GetIntendedTypeName(
            leftIsNull ? right.Expression : left.Expression
        );
        left = PromoteSetFieldToNullable(left, type, intendedType);
        right = PromoteSetFieldToNullable(right, type, intendedType);
        return true;
    }

    private static bool TryReconcileNullableSetFields(ref FieldNode left, ref FieldNode right)
    {
        var leftType = left.Expression.ReturnType;
        var rightType = right.Expression.ReturnType;
        if (leftType == null || rightType == null)
            return false;

        var leftUnderlying = Nullable.GetUnderlyingType(leftType);
        var rightUnderlying = Nullable.GetUnderlyingType(rightType);
        if (leftUnderlying != rightType && rightUnderlying != leftType)
            return false;

        var nullableType = leftUnderlying != null ? leftType : rightType;
        left = PromoteSetFieldToNullable(left, nullableType);
        right = PromoteSetFieldToNullable(right, nullableType);
        return true;
    }

    private static FieldNode PromoteSetFieldToNullable(FieldNode field, Type nullableType, string? intendedType = null)
    {
        if (field.Expression.ReturnType == nullableType)
            return field;

        // A projected null-only column has the same contextual value as the literal.
        if (field.Expression is NullNode || field.Expression.ReturnType is NullNode.NullType)
            return new FieldNode(
                new NullNode(nullableType, field.Expression.Span) { IntendedTypeName = intendedType },
                field.FieldOrder,
                field.FieldName,
                field.Span
            );

        var expression = new CastNode(
            field.Expression,
            Nullable.GetUnderlyingType(nullableType)!.Name,
            nullableType
        ).WithSpan(field.Expression.Span);
        return new FieldNode(expression, field.FieldOrder, field.FieldName, field.Span);
    }
}
