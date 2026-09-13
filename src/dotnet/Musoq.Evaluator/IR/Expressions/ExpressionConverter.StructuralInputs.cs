using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private IrExpression ConvertRecordLiteral(RecordLiteralNode node, Type? expectedType)
    {
        var descriptor = TryGetDescriptor(expectedType, StructuralTypeKind.Record);
        var targetType = descriptor?.ClrType is { } concrete && concrete != typeof(StructuralValue)
            ? concrete
            : null;
        var fields = node.Fields
            .Select(field => new StructuralFieldExpression(
                field.Name,
                Convert(field.Expression, FindFieldType(descriptor, field.Name))))
            .ToArray();
        return new StructuralRecordLiteral(targetType ?? typeof(StructuralValue), fields, targetType);
    }

    private IrExpression ConvertArrayLiteral(ArrayLiteralNode node, Type? expectedType)
    {
        var descriptor = TryGetDescriptor(expectedType, StructuralTypeKind.Collection);
        var elementType = descriptor?.ElementType?.ClrType ?? InferElementType(node);
        var returnType = expectedType ?? elementType.MakeArrayType();
        var elements = node.Elements
            .Select(element => Convert(element, elementType))
            .ToArray();
        return new StructuralArrayLiteral(returnType, elementType, elements);
    }

    private static StructuralTypeDescriptor? TryGetDescriptor(Type? type, StructuralTypeKind expectedKind)
    {
        if (type == null || type == typeof(StructuralValue) || type == typeof(object))
            return null;

        var descriptor = StructuralTypeDescriptor.FromClrType(type);
        return descriptor.Kind == expectedKind ? descriptor : null;
    }

    private static Type? FindFieldType(StructuralTypeDescriptor? descriptor, string name)
    {
        if (descriptor?.Fields == null)
            return null;

        return descriptor.Fields
            .FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
            ?.Type.ClrType;
    }

    private static Type InferElementType(ArrayLiteralNode node)
    {
        var types = node.Elements
            .Select(element => element.ReturnType)
            .Where(static type => type != null)
            .Cast<Type>()
            .Distinct()
            .ToArray();
        return types.Length == 1 ? types[0] : typeof(object);
    }
}