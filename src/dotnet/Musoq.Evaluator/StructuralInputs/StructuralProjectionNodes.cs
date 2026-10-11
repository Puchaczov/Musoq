using System;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator;

internal sealed class StructuralAccessColumnNode(string name, string alias, StructuralTypeDescriptor shape, TextSpan span)
    : AccessColumnNode(name, alias, shape.CoreValueType, span)
{
    internal StructuralTypeDescriptor Shape { get; } = shape;
}

internal sealed class StructuralPropertyValueNode(string name, StructuralTypeDescriptor shape, int index)
    : PropertyValueNode(name, typeof(StructuralPropertyMetadata<>).MakeGenericType(shape.CoreValueType).GetProperty("Value"))
{
    internal StructuralTypeDescriptor Shape { get; } = shape;
    internal int Index { get; } = index;
}

internal sealed class StructuralPropertyMetadata<T>
{
    public T Value => throw new InvalidOperationException("Structural member metadata cannot be invoked during execution.");
}
