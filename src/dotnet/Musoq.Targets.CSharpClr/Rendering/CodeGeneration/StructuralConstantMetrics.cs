using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

/// <summary>Measures compile-time structural defaults before emitting concrete storage.</summary>
internal static class StructuralConstantMetrics
{
    internal static StructuralInputMetrics MeasureConstant(
        object? value,
        StructuralTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return MeasureConstant(value, descriptor, 1);
    }

    private static StructuralInputMetrics MeasureConstant(
        object? value,
        StructuralTypeDescriptor descriptor,
        int depth)
    {
        var metrics = new StructuralInputMetrics(depth, 1, 0);
        if (value is StructuralValue scalar && scalar.Kind == StructuralTypeKind.Scalar)
            value = scalar.Scalar;
        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            if (value is string text)
                metrics = metrics with { StringBytes = checked((long)text.Length * sizeof(char)) };
            return metrics;
        }

        if (value == null)
            return metrics;

        if (descriptor.Kind == StructuralTypeKind.Record &&
            value is StructuralValue { Kind: StructuralTypeKind.Record } record)
        {
            foreach (var field in descriptor.Fields)
            {
                if (record.Fields.TryGetValue(field.Name, out var fieldValue))
                    metrics = Merge(metrics, MeasureConstant(fieldValue, field.Type, depth + 1));
                else if (field.HasDefault)
                    metrics = Merge(metrics, MeasureConstant(field.Default.Value, field.Type, depth + 1));
            }

            return metrics;
        }

        if (descriptor.Kind == StructuralTypeKind.Collection)
        {
            IEnumerable<object?> elements;
            if (value is StructuralValue { Kind: StructuralTypeKind.Collection } structural)
                elements = structural.Elements.Cast<object?>();
            else if (value is Array array)
                elements = array.Cast<object?>();
            else
                elements = [];
            var elementType = descriptor.ElementType ?? throw new InvalidOperationException("Collection element type is missing.");
            foreach (var element in elements)
                metrics = Merge(metrics, MeasureConstant(element, elementType, depth + 1));
        }

        return metrics;
    }

    private static StructuralInputMetrics Merge(
        StructuralInputMetrics left,
        StructuralInputMetrics right) =>
        new(
            Math.Max(left.MaxDepth, right.MaxDepth),
            checked(left.NodeCount + right.NodeCount),
            checked(left.StringBytes + right.StringBytes));

}
