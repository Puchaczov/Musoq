using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
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

    private static FieldNode PromoteSetFieldToNullable(FieldNode field, Type nullableType)
    {
        if (field.Expression.ReturnType == nullableType)
            return field;

        var expression = new CastNode(field.Expression, Nullable.GetUnderlyingType(nullableType)!.Name, nullableType)
            .WithSpan(field.Expression.Span);
        return new FieldNode(expression, field.FieldOrder, field.FieldName, field.Span);
    }
}
