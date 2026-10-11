using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    internal const string PartialPayloadQuery = "binary Packet { Amount: int le, Currency: byte }; " +
        "select s.Position as Position, p.ParsedFields as ParsedFields, p.ErrorField as ErrorField, " +
        "p.BytesConsumed as BytesConsumed from #schema.first() s " +
        "cross apply PartialInterpret<Packet>(s.Data) p order by s.Position";

    [TestMethod]
    public void TypedPartialPayload_WhenTheLastFieldFails_ShouldRetainConcreteValuesAndCompletionState()
    {
        PartialBytes[] input = [new(2, [42, 0, 0, 0]), new(1, [42, 0, 0, 0, 7])];
        using var compiled = CreateAndRunVirtualMachine<PartialBytes, LibraryBase>(PartialPayloadQuery, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        var payloadType = PartialPayloadAssertions.ColumnType(table, "Packet");
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)),
            ("ParsedFields", payloadType), ("ErrorField", typeof(string)), ("BytesConsumed", typeof(int)));
        Assert.AreEqual(typeof(int), payloadType.GetProperty("Amount")!.PropertyType);
        Assert.AreEqual(typeof(byte), payloadType.GetProperty("Currency")!.PropertyType);
        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        var complete = PartialPayloadAssertions.CompletedValues(rows[0].ReadValue(1));
        var failed = PartialPayloadAssertions.CompletedValues(rows[1].ReadValue(1));
        Assert.HasCount(2, complete);
        Assert.HasCount(1, failed);
        Assert.AreEqual(42, complete["Amount"]);
        Assert.AreEqual((byte)7, complete["Currency"]);
        Assert.AreEqual(42, failed["Amount"]);
        Assert.IsFalse(failed.ContainsKey("Currency"));
        CollectionAssert.AreEqual(new string?[] { null, "Currency" }, rows.Select(row => row.GetValue<string?>(2)).ToArray());
        CollectionAssert.AreEqual(new[] { 5, 4 }, rows.Select(row => row.GetValue<int>(3)).ToArray());
    }

    [TestMethod]
    public void PartialPayloadJsonQuery_ShouldSerializeOnlyCompletedConcreteFields()
    {
        const string query = "binary Packet { Amount: int le, Currency: byte }; " +
            "select s.Position as Position, ToJson(p.ParsedFields) as Json from #schema.first() s " +
            "cross apply PartialInterpret<Packet>(s.Data) p order by s.Position";
        PartialBytes[] input = [new(1, [42, 0, 0, 0, 7]), new(2, [42, 0, 0, 0])];
        using var compiled = CreateAndRunVirtualMachine<PartialBytes, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Json", typeof(string)));
        Assert.HasCount(2, rows);
        Assert.AreEqual(1, rows[0].GetValue<int>(0));
        Assert.AreEqual(2, rows[1].GetValue<int>(0));
        Assert.AreEqual("{\"Amount\":42,\"Currency\":7}", rows[0].GetValue<string>(1));
        Assert.AreEqual("{\"Amount\":42}", rows[1].GetValue<string>(1));
    }

    [TestMethod]
    public void PayloadJsonQuery_WhenFieldNamesMatchWriterLocals_ShouldReadTheNativeFields()
    {
        const string query = "binary Packet { fieldValue: int le, options: byte }; " +
            "select ToJson(p.ParsedFields) as Json from #schema.first() s cross apply PartialInterpret<Packet>(s.Data) p";
        using var compiled = CreateAndRunVirtualMachine<PartialBytes, LibraryBase>(query, [new(1, [42, 0, 0, 0, 7])]);
        using var table = compiled.Run(TestContext.CancellationToken);
        var row = table.Single();
        TableMaterializationTestHelper.AssertColumns(table, ("Json", typeof(string)));
        Assert.AreEqual("{\"fieldValue\":42,\"options\":7}", row.GetValue<string>(0));
    }

    public sealed record PartialBytes(int Position, byte[] Data);
}
