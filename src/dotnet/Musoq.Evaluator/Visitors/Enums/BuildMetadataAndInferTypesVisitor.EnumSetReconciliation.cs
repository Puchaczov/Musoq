using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private FieldNode PromoteEnumFieldToNullable(FieldNode field, EnumTypeDescriptor enumType)
    {
        var nullableType = MakeNullableEnumCarrierType(field.Expression.ReturnType, enumType);
        if (field.Expression.ReturnType == nullableType)
            return field;

        if (field.Expression is AccessColumnNode accessColumn)
        {
            accessColumn.ChangeReturnType(nullableType);
            return field;
        }

        if (field.Expression is NullNode)
        {
            var contextualNull = new NullNode(nullableType, field.Expression.Span);
            MarkEnumExpression(contextualNull, enumType);
            return new FieldNode(contextualNull, field.FieldOrder, field.FieldName, field.Span);
        }

        return field;
    }

    private static Type MakeNullableEnumCarrierType(Type? type, EnumTypeDescriptor enumType)
    {
        var carrierType = type ?? EnumScalarTypeFacts.GetCarrierType(enumType.UnderlyingKind);
        var underlyingType = Nullable.GetUnderlyingType(carrierType) ?? carrierType;
        return underlyingType.IsValueType && Nullable.GetUnderlyingType(carrierType) == null
            ? typeof(Nullable<>).MakeGenericType(underlyingType)
            : carrierType;
    }

    private static bool IsNullableValueType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }
}
