using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionBoundaryQueryTestsFixtures
{
    internal const string NativeRecordQuery =
        "select (Position: Position, Detail: (Value: Value), Items: array { Position, Position + 1 }) as Payload "
        + "from #schema.first() order by Position";

    internal struct NativeRecordVisitor(StructuralTypeDescriptor shape) : IColumnValueVisitor
    {
        internal Type? ValueType;
        internal StructuralValue? Value;
        internal string? Json;

        public void Visit<T>(int columnIndex, in T value)
        {
            Assert.AreEqual(0, columnIndex);
            ValueType = typeof(T);
            Value = NativeStructuralProjectionAssertions.Read(value, shape);
            Json = JsonSerializer.Serialize(value);
            var restored = JsonSerializer.Deserialize<T>(Json);
            Assert.AreEqual(Json, JsonSerializer.Serialize(restored));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>("{}"));
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<T>(
                    "{\"Position\":1,\"Position\":2,\"Detail\":{\"Value\":10},\"Items\":[1,2]}"
                )
            );
        }
    }

    public sealed record NativeRecordInput(int Position, int? Value);
}
