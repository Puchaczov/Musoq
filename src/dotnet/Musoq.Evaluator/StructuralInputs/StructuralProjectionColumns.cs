using System;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator;

internal sealed class StructuralProjectionColumn(string name, int index, StructuralTypeDescriptor shape)
    : SchemaColumn(name, index, shape.CoreValueType)
{
    internal StructuralTypeDescriptor Shape { get; } = shape;
}

internal static class StructuralProjectionColumns
{
    internal static ISchemaColumn Rename(ISchemaColumn column, string name)
    {
        ISchemaColumn renamed = column is StructuralProjectionColumn structural
            ? new StructuralProjectionColumn(name, column.ColumnIndex, structural.Shape)
            : new SchemaColumn(name, column.ColumnIndex, column.ColumnType, column.SourceReadType, column.EnumType,
                column.IntendedTypeName, column.ReadModifiers, column.Stability);
        return NullableInterpretationColumn.Preserve(renamed, column is NullableInterpretationColumn);
    }

    internal static StructuralTypeDescriptor? Describe(Node node) => node switch
    {
        RecordLiteralNode record => StructuralTypeDescriptor.Record(typeof(StructuralValue), record.Fields.Select(field =>
            new StructuralFieldDescriptor(field.Name, DescribeValue(field.Expression), true))),
        StructuralAccessColumnNode column => column.Shape,
        DotNode { Expression: StructuralPropertyValueNode property } => property.Shape,
        _ => null
    };

    private static StructuralTypeDescriptor DescribeValue(Node node)
    {
        if (Describe(node) is { } record)
            return record;
        if (node is ArrayLiteralNode array)
        {
            var expression = (StructuralArrayLiteral)new ExpressionConverter().Convert(array);
            var element = array.Elements.Count > 0 ? DescribeValue(array.Elements[0])
                : StructuralTypeDescriptor.FromClrType(expression.ElementType);
            return StructuralTypeDescriptor.Collection(expression.ReturnType, element);
        }
        var type = node.ReturnType is NullNode.NullType ? typeof(DBNull) : node.ReturnType ??
            throw new InvalidOperationException("A structural field requires an inferred type.");
        return StructuralTypeDescriptor.Scalar(Nullable.GetUnderlyingType(type) ?? type,
            !type.IsValueType || Nullable.GetUnderlyingType(type) is not null);
    }
}
