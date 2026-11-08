using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

/// <summary>Inspects runtime-generated native records in tests, preserving independent presence/value assertions.</summary>
internal static class NativeStructuralProjectionAssertions
{
    internal static StructuralTypeDescriptor IdRecord => StructuralTypeDescriptor.Record(typeof(StructuralValue),
        [new("Id", StructuralTypeDescriptor.Scalar(typeof(int)), true)]);

    internal static StructuralTypeDescriptor IdAndValueRecord => StructuralTypeDescriptor.Record(typeof(StructuralValue),
        [new("Id", StructuralTypeDescriptor.Scalar(typeof(int)), true), new("Value", StructuralTypeDescriptor.Scalar(typeof(int)), true)]);

    internal static StructuralTypeDescriptor NestedRecord => StructuralTypeDescriptor.Record(typeof(StructuralValue),
        [new("Payload", IdRecord, true),
            new("Items", StructuralTypeDescriptor.Collection(typeof(int[]), StructuralTypeDescriptor.Scalar(typeof(int))), true),
            new("Mode", StructuralTypeDescriptor.Scalar(typeof(DBNull), nullable: true), true)]);

    internal static StructuralTypeDescriptor RecordsWithOptionalMode => StructuralTypeDescriptor.Collection(typeof(StructuralValue[]),
        StructuralTypeDescriptor.Record(typeof(StructuralValue),
            [new("Id", StructuralTypeDescriptor.Scalar(typeof(int)), true),
                new("Mode", StructuralTypeDescriptor.Scalar(typeof(string), nullable: true), false)]));

    internal static StructuralValue Read(object? value, StructuralTypeDescriptor shape)
    {
        if (value is null)
            return StructuralValue.FromScalar(null);
        Assert.IsNull(ConcreteValueShape.FindUnresolvedMember(value.GetType()));
        if (shape.Kind == StructuralTypeKind.Scalar)
        {
            Assert.AreEqual(shape.ScalarType, value.GetType());
            return StructuralValue.FromScalar(value);
        }
        if (shape.Kind == StructuralTypeKind.Collection)
        {
            Assert.IsInstanceOfType<Array>(value);
            var values = new List<StructuralValue>();
            foreach (var element in (Array)value)
                values.Add(Read(element, shape.ElementType!));
            return StructuralValue.FromCollection(values);
        }
        var type = value.GetType();
        Assert.IsTrue(type.IsValueType);
        Assert.StartsWith("__musoqStructuralCarrier_", type.Name);
        var fields = new List<KeyValuePair<string, StructuralValue>>();
        for (var index = 0; index < shape.Fields.Count; index++)
        {
            var presence = (ulong)type.GetField($"P{index / 64}")!.GetValue(value)!;
            if ((presence & (1UL << (index % 64))) == 0)
                continue;
            var field = shape.Fields[index];
            fields.Add(new(field.Name, Read(type.GetField($"F{index}")!.GetValue(value), field.Type)));
        }
        return StructuralValue.FromRecord(fields);
    }
}
