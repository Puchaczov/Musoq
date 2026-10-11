using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionBoundaryQueryTests
{
    internal const string NativeRecordQuery = "select (Position: Position, Detail: (Value: Value), Items: array { Position, Position + 1 }) as Payload " +
        "from #schema.first() order by Position";

    [TestMethod]
    [DataRow("Records", "Payload")]
    [DataRow("Records (Renamed)", "Renamed")]
    public void NativeRecordThroughCte_WhenSelectingNestedMembers_ShouldPreserveConcreteNullableValues(string declaration, string payload)
    {
        var query = $"with {declaration} as (select (Position: Position, Detail: (Value: Value)) as Payload " +
            $"from #schema.first()) select {payload}.Position as Position, {payload}.Detail.Value as Value from Records order by Position";
        using var compiled = CreateAndRunVirtualMachine<NativeRecordInput, LibraryBase>(query, [new(1, 10), new(2, null)]);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Value", typeof(int?)));
        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new int?[] { 10, null }, rows.Select(row => row.GetValue<int?>(1)).ToArray());
    }

    [TestMethod]
    public void InlineNativeRecord_WhenVisited_ShouldPreserveNestedConcreteTypesAndNullableValues()
    {
        NativeRecordInput[] input = [new(1, 10), new(2, null)];
        using var compiled = CreateAndRunVirtualMachine<NativeRecordInput, LibraryBase>(NativeRecordQuery, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(2, rows);
        var column = table.Columns.Single();
        Assert.AreEqual("Payload", column.ColumnName);
        Assert.IsTrue(column.ColumnType.IsValueType);
        Assert.IsNull(ConcreteValueShape.FindUnresolvedMember(column.ColumnType));
        var shape = StructuralTypeDescriptor.Record(typeof(StructuralValue),
            [new("Position", StructuralTypeDescriptor.Scalar(typeof(int)), true),
                new("Detail", StructuralTypeDescriptor.Record(typeof(StructuralValue),
                    [new("Value", StructuralTypeDescriptor.Scalar(typeof(int), nullable: true), true)]), true),
                new("Items", StructuralTypeDescriptor.Collection(typeof(int[]), StructuralTypeDescriptor.Scalar(typeof(int))), true)]);
        for (var index = 0; index < rows.Length; index++)
        {
            var visitor = new NativeRecordVisitor(shape);
            rows[index].VisitColumns(ref visitor);
            Assert.AreEqual(column.ColumnType, visitor.ValueType);
            Assert.AreEqual(index + 1, visitor.Value!.Fields["Position"].Scalar);
            Assert.AreEqual(input[index].Value, visitor.Value.Fields["Detail"].Fields["Value"].Scalar);
            CollectionAssert.AreEqual(new[] { index + 1, index + 2 },
                visitor.Value.Fields["Items"].Elements.Select(item => (int)item.Scalar!).ToArray());
            Assert.AreEqual(index == 0
                ? "{\"Position\":1,\"Detail\":{\"Value\":10},\"Items\":[1,2]}"
                : "{\"Position\":2,\"Detail\":{\"Value\":null},\"Items\":[2,3]}", visitor.Json);
        }
    }

    private struct NativeRecordVisitor(StructuralTypeDescriptor shape) : IColumnValueVisitor
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
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(
                "{\"Position\":1,\"Position\":2,\"Detail\":{\"Value\":10},\"Items\":[1,2]}"));
        }
    }

    public sealed record NativeRecordInput(int Position, int? Value);
}
