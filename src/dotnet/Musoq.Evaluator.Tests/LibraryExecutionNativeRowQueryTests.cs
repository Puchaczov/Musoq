using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionNativeRowQueryTests : GenericEntityTestBase
{
    [TestMethod]
    [DataRow("s.Number = 1", "1,2,5,6")]
    [DataRow("s.Number = 999", "")]
    [DataRow("s.Number = 1 and s.Text = 'A'", "1,2")]
    [DataRow("s.Number = 1 and s.Text = 'B'", "5")]
    [DataRow("s.Number = 2 and s.Text = 'A'", "3")]
    [DataRow("s.Number = 1 and s.Other = 3", "1,2,6")]
    [DataRow("s.Number = 3 and s.Other = 1", "")]
    [DataRow("s.Number is null", "4,7")]
    [DataRow("s.Number is null and s.Text = 'A'", "4")]
    [DataRow("s.Number is not null and s.Text = 'A'", "1,2,3")]
    [DataRow("s.Number = 1 and s.Text = 'a'", "6")]
    [DataRow("s.Text is null", "7")]
    [DataRow("s.Number is null and s.Other is null", "7")]
    [DataRow("s.Other = 3 and s.Text = 'A'", "1,2,3")]
    public void NativeLookup_WhenKeysVary_ShouldPreserveExactMatches(string predicate, string expectedIds)
    {
        var query = $"select s.Position as Position from #schema.first() s where {predicate} order by Position";
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
        var actual = table.Select(row => row.GetValue<int>(0)).ToArray();
        var expected = expectedIds.Length == 0 ? Array.Empty<int>() : expectedIds.Split(',').Select(int.Parse).ToArray();
        CollectionAssert.AreEqual(expected, actual);
        Assert.AreEqual(expected.Length, table.Count);
    }

    [TestMethod]
    public void NativeGrouping_WhenCompositeKeysContainNullsAndDuplicates_ShouldKeepSeparateGroups()
    {
        const string query = "select s.Number as Number, s.Text as Text, Count(s.Position) as Total " +
            "from #schema.first() s group by s.Number, s.Text";
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Number", typeof(int?)), ("Text", typeof(string)), ("Total", typeof(long)));
        var actual = table.Select(row => (row.GetValue<int?>(0), row.GetValue<string>(1), row.GetValue<long>(2))).ToArray();
        (int?, string?, long)[] expected = [(1, "A", 2), (2, "A", 1), (null, "A", 1), (1, "B", 1), (1, "a", 1), (null, null, 1)];
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual(6, table.Count);
    }

    [TestMethod]
    public void NativeResults_WhenEnumeratedAndDisposed_ShouldPreserveValuesAcrossIndependentExecutions()
    {
        const string query = "with rows as (select s.Position as Position from #schema.first() s) " +
            "select r.Position as Position from rows r order by Position";
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(query, Rows());
        for (var iteration = 0; iteration < 3; iteration++)
        {
            using var table = compiled.Run(TestContext.CancellationToken);
            TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, table.Select(row => row.GetValue<int>(0)).ToArray());
            Assert.AreEqual(7, table.Count);
        }
    }

    public const string CompositeJoinQuery = "select a.Position as LeftPosition, b.Position as RightPosition " +
        "from #schema.first() a inner join #schema.first() b on a.Number = b.Number and a.Text = b.Text " +
        "order by LeftPosition, RightPosition";

    [TestMethod]
    public void NativeJoin_WhenCompositeKeysHaveDuplicates_ShouldReturnEveryMatchingOccurrence()
    {
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(CompositeJoinQuery, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("LeftPosition", typeof(int)), ("RightPosition", typeof(int)));
        var actual = table.Select(row => (row.GetValue<int>(0), row.GetValue<int>(1))).ToArray();
        (int, int)[] expected = [(1, 1), (1, 2), (2, 1), (2, 2), (3, 3), (5, 5), (6, 6)];
        CollectionAssert.AreEqual(expected, actual);
        Assert.AreEqual(expected.Length, table.Count);
    }

    [TestMethod]
    public void NativeSetEquality_WhenCompositeValuesContainNulls_ShouldDeduplicateEqualRows()
    {
        const string query = "select s.Number as Number, s.Text as Text from #schema.first() s " +
            "union select t.Number as Number, t.Text as Text from #schema.first() t";
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Number", typeof(int?)), ("Text", typeof(string)));
        var actual = table.Select(row => (row.GetValue<int?>(0), row.GetValue<string>(1))).ToArray();
        (int?, string?)[] expected = [(1, "A"), (2, "A"), (null, "A"), (1, "B"), (1, "a"), (null, null)];
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual(6, table.Count);
    }

    [TestMethod]
    public void NativeResultAssignment_WhenTypesAreConcrete_ShouldWriteAndRejectAnIncompatibleValue()
    {
        const string query = "select p.Position as Position from #schema.first() p where p.Position = 1";
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
        var row = table.Single();
        row.AssignValue(0, 31);
        Assert.AreEqual(31, row.GetValue<int>(0));
        Assert.Throws<InvalidOperationException>(() => row.AssignValue(0, 2.5d));
        Assert.AreEqual(31, row.GetValue<int>(0));
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void NativeTypedGrouping_WhenPublicRowCarrierIsUnused_ShouldConsumeConcreteDtoResults()
    {
        const string query = NativeTypedGroupingQuery;
        var provider = Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider(Rows());
        var compiled = Musoq.Converter.InstanceCreator.CompileForTypedExecution<NativeSubtotal>(query,
            Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();
        Assert.HasCount(3, rows);
        CollectionAssert.AreEquivalent(new NativeSubtotal[] { new(null, 2), new(1, 4), new(2, 1) }, rows);
    }

    public const string NativeTypedGroupingQuery = "select Number, Count(Position) as Total from #schema.first() group by Number";

    public readonly record struct NativeSubtotal(int? Number, long Total);

    [TestMethod]
    public void NativeRowContracts_ShouldExcludeRetiredErasedIndexAndKeyApis()
    {
        Assert.IsEmpty(typeof(Row).GetProperties().Where(property => property.GetIndexParameters().Length != 0).ToArray());
        Assert.IsEmpty(typeof(IReadOnlyRow).GetProperties().Where(property => property.GetIndexParameters().Length != 0).ToArray());
        foreach (var name in new[] { "Key", "IndexedList`2", "IValue`1" })
            Assert.IsNull(typeof(Row).Assembly.GetType("Musoq.Evaluator.Tables." + name));
        Assert.AreEqual(typeof(object), typeof(Table).BaseType);
    }

    [TestMethod]
    public void NativeResultReadAndVisit_WhenNumeric_ShouldAllocateNoBoxes()
    {
        const string query = "select s.Position as Position from #schema.first() s where s.Position = 1";
        using var compiled = CreateAndRunVirtualMachine<NativeKeyInput>(query, Rows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
        var row = table.Single();
        var (allocated, checksum) = AllocationTestThread.Run(() => MeasureNativeNumericReads(row));
        Assert.AreEqual(200_000L, checksum);
        Assert.AreEqual(0L, allocated);
    }

    private static (long Allocated, long Checksum) MeasureNativeNumericReads(Row row)
    {
        var visitor = new NumericVisitor();
        for (var index = 0; index < 10_000; index++)
        {
            _ = row.GetValue<int>(0);
            row.VisitColumns(ref visitor);
        }
        visitor.Total = 0;
        long checksum = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100_000; index++)
        {
            checksum += row.GetValue<int>(0);
            row.VisitColumns(ref visitor);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        return (allocated, checksum + visitor.Total);
    }

    private struct NumericVisitor : IColumnValueVisitor
    {
        public long Total;
        public void Visit<T>(int columnIndex, in T value)
        {
            if (typeof(T) != typeof(int)) throw new InvalidOperationException("Expected concrete int column.");
            Total += System.Runtime.CompilerServices.Unsafe.As<T, int>(ref System.Runtime.CompilerServices.Unsafe.AsRef(in value));
        }
    }

    [TestMethod]
    public void NativeSource_WhenAnUnselectedMemberIsErased_ShouldReadOnlyItsConcretePath()
    {
        const string query = "select p.Payload.Value as Value from #schema.first() p";
        using var compiled = CreateAndRunVirtualMachine<OpaqueInput>(query, [new(new OpaquePayload("typed", 42))]);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        Assert.AreEqual("typed", table.Single().GetValue<string>(0));
    }

    [TestMethod]
    public void NativeSource_WhenTheErasedMemberIsSelected_ShouldRejectItsValueContract()
    {
        const string query = "select p.Payload.Unresolved as Value from #schema.first() p";
        var error = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<OpaqueInput>(query, [new(new OpaquePayload("typed", 42))]));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Unresolved", error.Message);
    }

    [TestMethod]
    public void NativeDictionary_WhenMembersAreNamedInSql_ShouldPreserveConcreteValuesAndMissingKeys()
    {
        const string query = NativeSignalQuery;
        var inputs = NativeSignalRows();
        using var compiled = CreateAndRunVirtualMachine<NativeSignalInput>(query, inputs);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Temperature", typeof(double?)));
        var rows = table.ToArray();
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new double?[] { 90d, null, null }, rows.Select(row => row.GetValue<double?>(1)).ToArray());
    }

    public const string NativeSignalQuery = "select p.Position as Position, p.Signals.Temperature as Temperature " +
        "from #schema.first() p order by Position";
    public static NativeSignalInput[] NativeSignalRows() =>
    [
        new(1, new System.Collections.Generic.Dictionary<string, double?> { ["Temperature"] = 90d }),
        new(2, new System.Collections.Generic.Dictionary<string, double?>()),
        new(3, null)
    ];

    public sealed record NativeSignalInput(int Position, System.Collections.Generic.IReadOnlyDictionary<string, double?>? Signals);

    [TestMethod]
    public void NativeScalar_WhenItsConcreteReferenceHasUnselectedErasedMembers_ShouldBindItsExactSignature()
    {
        const string query = "select p.PayloadText(p.Payload) as Value from #schema.first() p";
        var provider = Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<OpaqueInput, OpaqueLibrary>(
            [new(new OpaquePayload("typed scalar", 42))]);
        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(query,
            Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        Assert.AreEqual("typed scalar", table.Single().GetValue<string>(0));
    }

    [TestMethod]
    public void NativeApply_WhenScalarReturnsConcreteReferences_ShouldReadOnlyTheSelectedValuePath()
    {
        const string query = "select x.Value as Value from #schema.first() p cross apply p.Payloads() x";
        var provider = Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<OpaqueInput, OpaqueLibrary>(
            [new(new OpaquePayload("input", 42))]);
        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(query,
            Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        Assert.AreEqual("typed apply", table.Single().GetValue<string>(0));
    }

    [TestMethod]
    public void NativeApply_WhenAnErasedResultMemberIsSelected_ShouldRejectThatValuePath()
    {
        const string query = "select x.Unresolved as Value from #schema.first() p cross apply p.Payloads() x";
        var provider = Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<OpaqueInput, OpaqueLibrary>(
            [new(new OpaquePayload("input", 42))]);
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            Musoq.Converter.InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"),
                provider, LoggerResolver, TestCompilationOptions));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, exception.PrimaryEnvelope.Code);
        Assert.Contains("Unresolved", exception.Message);
    }

    public sealed class OpaqueLibrary : Musoq.Plugins.LibraryBase
    {
        [Musoq.Plugins.Attributes.BindableMethod]
        public string PayloadText(OpaquePayload payload) => payload.Value;
        [Musoq.Plugins.Attributes.BindableMethod]
        public System.Collections.Generic.IEnumerable<OpaquePayload> Payloads() => [new("typed apply", 42)];
    }

    public sealed record OpaqueInput(OpaquePayload Payload);
    public sealed record OpaquePayload(string Value, object Unresolved);

    public const string NativePayloadQuery = "select p.Position as Position, p.Payload.Value as Value " +
        "from #positional.all() p order by Position";

    [TestMethod]
    public void NativeSourcePayload_WhenTheParentIsMissing_ShouldReadConcreteNullableFields()
    {
        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(NativePayloadQuery,
            Guid.NewGuid().ToString("N"), PayloadProvider(), LoggerResolver);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Value", typeof(decimal?)));
        var rows = table.ToArray();
        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 7m, null }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }

    public sealed record NativePayload(decimal? Value);
    public static Musoq.Tests.Common.Schema.PositionalRowsSchemaProvider PayloadProvider() => new(
        [new Musoq.Schema.DataSources.SchemaColumn("Payload", 0, typeof(NativePayload)),
         new Musoq.Schema.DataSources.SchemaColumn("Position", 1, typeof(int))],
        [[new NativePayload(7m), 1], [null!, 2]]);

    public sealed record NativeKeyInput(int Position, int? Number, string? Text, int? Other);
    public static NativeKeyInput[] Rows() =>
    [
        new(1, 1, "A", 3), new(2, 1, "A", 3), new(3, 2, "A", 3), new(4, null, "A", 2),
        new(5, 1, "B", 4), new(6, 1, "a", 3), new(7, null, null, null)
    ];
}
