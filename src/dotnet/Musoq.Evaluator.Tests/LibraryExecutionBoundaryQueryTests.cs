using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionBoundaryQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void RuntimeResult_WhenVisited_ShouldPreserveNullableNumericValuesAndVisitorState()
    {
        const string query = "select Position, Amount from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var visitor = new NumericVisitor();
        foreach (var row in result)
            row.VisitColumns(ref visitor);

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Amount", typeof(decimal?)));
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual(10, visitor.Values);
        Assert.AreEqual(15, visitor.Positions);
        Assert.AreEqual(6m, visitor.Amounts);
        Assert.AreEqual(1, visitor.Nulls);
    }

    [TestMethod]
    public void MetadataResult_WhenProjectedToWritableStruct_ShouldConstructConcreteValues()
    {
        const string query = "desc functions #schema";
        var compiled = InstanceCreator.CompileForTypedProfile<FunctionRow>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        var rows = compiled.RunWithProfile(TestContext.CancellationToken).Rows.ToArray();

        Assert.IsTrue(rows.Length > 500);
        Assert.IsTrue(rows.Any(static row => row.Method.Contains("PositiveTotal(")));
        Assert.IsTrue(rows.All(static row => row.Method.Length > 0 && row.Source.Length > 0));
    }

    [TestMethod]
    public void TypedResult_WhenReadByDeclaredColumnType_ShouldReturnValuesWithoutLegacyCells()
    {
        const string query = "select Sum(Amount) as Total from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(decimal?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(6m, rows[0].GetValue<decimal?>(0));
    }

    [TestMethod]
    public void TypedSource_WhenItsValueTypeIsUnresolved_ShouldReportTheProviderContract()
    {
        const string query = "select ToDecimal(Value) as Amount from #schema.first()";
        var provider = LibraryExecutionFixture.CreateProvider<ObjectFieldEntity>([new(1m)]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<AmountRow>(query, Guid.NewGuid().ToString(), provider, LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("Value", error.Message);
        Assert.Contains("concrete value type", error.Message);
    }

    [TestMethod]
    public void TypedSource_WhenNestedValuesEraseTheirTypes_ShouldReportTheNestedMember()
    {
        const string query = "select ToDecimal(Payload.Value) as Amount from #schema.first()";
        var provider = LibraryExecutionFixture.CreateProvider<NestedObjectEntity>([new(new(1m))]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<AmountRow>(query, Guid.NewGuid().ToString(), provider, LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("ObjectFieldEntity.Value", error.Message);
    }

    [TestMethod]
    public void TupleResult_WhenAliasesAreDomainNames_ShouldBindPositionallyWithConcreteTypes()
    {
        const string query = "select Position, Amount from #schema.first() order by Position";
        var compiled = InstanceCreator.CompileForTypedExecution<(int Position, decimal? Amount)>(query,
            Guid.NewGuid().ToString(), LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        CollectionAssert.AreEqual(new (int, decimal?)[]
        {
            (1, 1.25m), (2, 2.75m), (3, null), (4, -3m), (5, 5m)
        }, rows);
    }

    [TestMethod]
    public void TypedScalarParameter_WhenExecuted_ShouldRetainItsDeclaredTypeAcrossRows()
    {
        const string query = "param(factor: int); select Position + $factor as Position, Amount " +
                             "from #schema.first() order by Position";
        var compiled = InstanceCreator.CompileForTypedExecution<(int, decimal?)>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set("factor", 10);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        CollectionAssert.AreEqual(new (int, decimal?)[]
        {
            (11, 1.25m), (12, 2.75m), (13, null), (14, -3m), (15, 5m)
        }, rows);
    }

    [TestMethod]
    public void TypedNullableParameter_WhenExplicitlyNull_ShouldPreserveNullAsASuppliedValue()
    {
        const string query = "param(bonus: decimal?); select $bonus as Amount from #schema.first() where Position = 1";
        var compiled = InstanceCreator.CompileForTypedExecution<AmountRow>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set<decimal?>("bonus", null);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        CollectionAssert.AreEqual(new[] { new AmountRow(null) }, rows);
    }

    [TestMethod]
    public void TypedCollectionParameter_WhenCallerMutatesItAfterRun_ShouldUseTheCapturedElements()
    {
        const string query = "param(ids: int[]); select Position, Amount from #schema.first() " +
                             "where Position in $ids order by Position";
        var compiled = InstanceCreator.CompileForTypedExecution<(int, decimal?)>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        var ids = new[] { 1, 2 };
        compiled.TypedParameters.SetCollection("ids", ids);
        var pendingRows = compiled.Run(TestContext.CancellationToken);
        ids[0] = 5;
        var rows = pendingRows.ToArray();

        CollectionAssert.AreEqual(new[] { (1, (decimal?)1.25m), (2, (decimal?)2.75m) }, rows);
    }

    [TestMethod]
    public void TypedParameter_WhenItsSuppliedTypeDoesNotMatchSql_ShouldReportATypeMismatch()
    {
        const string query = "param(factor: int); select Position + $factor as Position from #schema.first()";
        var compiled = InstanceCreator.CompileForTypedExecution<PositionRow>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set("factor", 10L);

        var error = Assert.Throws<QueryExecutionException>(() => compiled.Run(TestContext.CancellationToken).ToArray());

        Assert.Contains("MQ7004", error.ToString());
        Assert.Contains("int", error.ToString());
        Assert.Contains("long", error.ToString());
    }

    [TestMethod]
    public void TypedStructuredParameter_WhenDecoded_ShouldUseConcreteNestedValuesAndFieldDefaults()
    {
        const string query = "param(options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int = 3))); " +
                             "select Enabled, Before, After from #inputs.configure(options: $options)";
        var compiled = InstanceCreator.CompileForTypedExecution<(bool, int, int)>(query, Guid.NewGuid().ToString(),
            new StructuredInputsSchemaProvider(), LoggerResolver);
        using (var document = JsonDocument.Parse("{\"Enabled\":true,\"Codes\":[10,20],\"Window\":{\"Before\":2}}"))
            compiled.TypedParameters.SetJson("options", document.RootElement);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        CollectionAssert.AreEqual(new[] { (true, 2, 3) }, rows);
    }

    [TestMethod]
    [DataRow("{\"Enabled\":true,\"Codes\":[10],\"Window\":{\"Before\":\"two\"}}", "MQ7004")]
    [DataRow("{\"Enabled\":true,\"Codes\":[10]}", "MQ7003")]
    [DataRow("{\"Enabled\":true,\"Codes\":[10],\"Window\":{\"Before\":null}}", "MQ7005")]
    [DataRow("{\"Enabled\":true,\"Codes\":[10],\"Window\":{\"Before\":2},\"Unexpected\":1}", "MQ7006")]
    [DataRow("{\"Enabled\":true,\"Codes\":[10],\"Window\":{\"Before\":2,\"before\":3}}", "MQ7004")]
    public void TypedStructuredParameter_WhenShapeIsInvalid_ShouldReportTheResponsibleDiagnostic(string json, string code)
    {
        const string query = "param(options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int = 3))); " +
                             "select Enabled, Before, After from #inputs.configure(options: $options)";
        var compiled = InstanceCreator.CompileForTypedExecution<(bool, int, int)>(query, Guid.NewGuid().ToString(),
            new StructuredInputsSchemaProvider(), LoggerResolver);
        using var document = JsonDocument.Parse(json);
        compiled.TypedParameters.SetJson("options", document.RootElement);

        var error = Assert.Throws<QueryExecutionException>(() => compiled.Run(TestContext.CancellationToken).ToArray());

        Assert.Contains(code, error.ToString());
    }

    [TestMethod]
    public void TypedStructuredCollection_WhenNestedArraysAreDecoded_ShouldPreserveTheirCoordinates()
    {
        const string query = "param(values: int[][]); select Value, Row, Column from #inputs.matrix(values: $values) order by Row, Column";
        var compiled = InstanceCreator.CompileForTypedExecution<(int, int, int)>(query, Guid.NewGuid().ToString(),
            new StructuredInputsSchemaProvider(), LoggerResolver);
        using var document = JsonDocument.Parse("[[1,2],[],[3]]");
        compiled.TypedParameters.SetJson("values", document.RootElement);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        CollectionAssert.AreEqual(new[] { (1, 0, 0), (2, 0, 1), (3, 2, 0) }, rows);
    }

    [TestMethod]
    public void TypedProfileParameter_WhenExecuted_ShouldUseTheSameConcreteSlots()
    {
        const string query = "param(bonus: decimal?); select Amount + $bonus as Amount from #schema.first() order by Position";
        var compiled = InstanceCreator.CompileForTypedProfile<AmountRow>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set<decimal?>("bonus", 1m);
        var result = compiled.RunWithProfile(TestContext.CancellationToken);
        var rows = result.Rows.ToArray();

        CollectionAssert.AreEqual(new[] { new AmountRow(2.25m), new AmountRow(3.75m), new AmountRow(null), new AmountRow(-2m), new AmountRow(6m) }, rows);
        Assert.IsNotNull(result.Profile);
    }

    [TestMethod]
    public void TypedSource_WhenFrameworkCollectionErasesItsElements_ShouldRejectTheUnresolvedShape()
    {
        const string query = "select ToString(Value) as Text from #schema.first()";
        var provider = LibraryExecutionFixture.CreateProvider<FrameworkCollectionEntity>([new(new ArrayList { 1 })]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<TextRow>(query, Guid.NewGuid().ToString(), provider, LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("ArrayList", error.Message);
    }

    [TestMethod]
    public void TypedColumns_WhenConsumedRepeatedly_ShouldAllocateNoBoxedValuesOrInvocationObjects()
    {
        const string query = "select Position, Amount from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        var measured = MeasureIsolatedColumnReads(rows);

        Assert.AreEqual(0L, measured.Allocated);
        Assert.AreEqual(150_000, measured.Positions);
        Assert.AreEqual(60_000m, measured.Amounts);
    }

    [TestMethod]
    public void TypedResult_WhenItsDeclaredMemberWouldBoxNumericValues_ShouldRejectTheErasedOutput()
    {
        const string query = "select Amount from #schema.first()";

        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<ErasedAmountRow>(query, Guid.NewGuid().ToString(),
                LibraryExecutionFixture.CreateProvider(), LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("ErasedAmountRow.Amount", error.Message);
    }

    [TestMethod]
    public void TypedProfileSource_WhenItsValueTypeIsUnresolved_ShouldRejectItBeforeExecution()
    {
        const string query = "select ToDecimal(Value) as Amount from #schema.first()";
        var provider = LibraryExecutionFixture.CreateProvider<ObjectFieldEntity>([new(1m)]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedProfile<AmountRow>(query, Guid.NewGuid().ToString(), provider, LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("concrete value type", error.Message);
    }

    [TestMethod]
    public void TypedScalarParameter_WhenLaterRunsChangeIt_ShouldKeepEachRunSnapshotIndependent()
    {
        const string query = "param(factor: int); select Position + $factor as Position from #schema.first() where Position = 1";
        var compiled = InstanceCreator.CompileForTypedExecution<PositionRow>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set("factor", 10);
        var first = compiled.Run(TestContext.CancellationToken);
        compiled.TypedParameters.Set("factor", 20);
        var second = compiled.Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(new[] { new PositionRow(11) }, first.ToArray());
        CollectionAssert.AreEqual(new[] { new PositionRow(21) }, second.ToArray());
    }

    [TestMethod]
    public void TypedParameter_WhenItIsNotDeclared_ShouldReportUnknownParameter()
    {
        const string query = "select Position from #schema.first()";
        var compiled = InstanceCreator.CompileForTypedExecution<PositionRow>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set("typo", 10);

        var error = Assert.Throws<QueryExecutionException>(() => compiled.Run(TestContext.CancellationToken).ToArray());

        Assert.Contains("MQ7006", error.ToString());
        Assert.Contains("typo", error.ToString());
    }

    [TestMethod]
    public void TypedParameters_WhenEveryInputUsesNativeSlots_ShouldExposeNoLegacyDictionary()
    {
        const string sql = "param(factor: int, bonus: decimal?); select Position + $factor as Position from #schema.first()";
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver);
        compiled.TypedParameters.Set("factor", 10);
        compiled.TypedParameters.Set<decimal?>("bonus", 1m);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
        CollectionAssert.AreEqual(new[] { 11, 12, 13, 14, 15 }, table.Select(row => row.GetValue<int>(0)).OrderBy(value => value).ToArray());
        Assert.IsNull(typeof(CompiledQuery).GetProperty("Parameters"));
        Assert.IsNull(typeof(CompiledTypedQuery<PositionRow>).GetProperty("Parameters"));
    }

    [TestMethod]
    public void TypedColumns_WhenSqlAliasesAreReservedWords_ShouldPreserveNamesAndConcreteValues()
    {
        const string query = "select Position as [case], Amount as [decimal] from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var visitor = new NumericVisitor();
        foreach (var row in result) row.VisitColumns(ref visitor);
        var positions = result.Select(static row => row.GetValue<int>(0)).ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("case", typeof(int)), ("decimal", typeof(decimal?)));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, positions);
        Assert.AreEqual(6m, visitor.Amounts);
        Assert.AreEqual(1, visitor.Nulls);
    }

    [TestMethod]
    [DataRow("T")]
    [DataRow("TVisitor")]
    [DataRow("GetValue")]
    [DataRow("VisitColumns")]
    [DataRow("VisitColumn")]
    public void TypedColumns_WhenAliasMatchesATypeParameterOrRowMethod_ShouldPreserveTheSqlName(string alias)
    {
        var query = $"select Position as [{alias}] from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var positions = result.Select(static row => row.GetValue<int>(0)).ToArray();

        TableMaterializationTestHelper.AssertColumns(result, (alias, typeof(int)));
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, positions);
    }

    [TestMethod]
    public void TypedStructuredParameter_WhenOmittedBesideASuppliedScalar_ShouldApplyItsDeclaredDefault()
    {
        const string query = "param(offset: int, options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int = 3)) = " +
                             "(Enabled: true, Codes: array { 10 }, Window: (Before: 2))); " +
                             "select Enabled, Before + $offset as Before, After from #inputs.configure(options: $options)";
        var compiled = InstanceCreator.CompileForTypedExecution<(bool, int, int)>(query, Guid.NewGuid().ToString(),
            new StructuredInputsSchemaProvider(), LoggerResolver);
        compiled.TypedParameters.Set("offset", 10);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();

        CollectionAssert.AreEqual(new[] { (true, 12, 3) }, rows);
    }

    public readonly record struct ErasedAmountRow(object Amount);

    private static (long Allocated, int Positions, decimal Amounts) MeasureIsolatedColumnReads(Row[] rows) =>
        AllocationTestThread.Run(() => MeasureColumnReads(rows));

    private static (long Allocated, int Positions, decimal Amounts) MeasureColumnReads(Row[] rows)
    {
        _ = GC.GetAllocatedBytesForCurrentThread();
        var warmVisitor = new AllocationVisitor();
        ConsumeColumns(rows, ref warmVisitor);
        ConsumeColumns(rows, ref warmVisitor);
        var visitor = new AllocationVisitor();
        var before = GC.GetAllocatedBytesForCurrentThread();
        ConsumeColumns(rows, ref visitor);
        return (GC.GetAllocatedBytesForCurrentThread() - before, visitor.Positions, visitor.Amounts);
    }

    private static void ConsumeColumns(Row[] rows, ref AllocationVisitor visitor)
    {
        for (var iteration = 0; iteration < 10_000; iteration++)
            foreach (var row in rows)
            {
                row.VisitColumns(ref visitor);
                if (row.GetValue<int>(0) <= 0) throw new InvalidOperationException("Invalid position.");
                _ = row.GetValue<decimal?>(1);
            }
    }

    public sealed record FrameworkCollectionEntity(ArrayList Value);
    public readonly record struct TextRow(string Text);

    private struct AllocationVisitor : IColumnValueVisitor
    {
        public int Positions;
        public decimal Amounts;

        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex == 0)
                Positions += Unsafe.As<T, int>(ref Unsafe.AsRef(in value));
            else
                Amounts += Unsafe.As<T, decimal?>(ref Unsafe.AsRef(in value)).GetValueOrDefault();
        }
    }

    public readonly record struct PositionRow(int Position);
    public sealed record ObjectFieldEntity(object Value);
    public sealed record NestedObjectEntity(ObjectFieldEntity Payload);
    public readonly record struct AmountRow(decimal? Amount);

    public struct FunctionRow
    {
        public string Method { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }
    }

    private struct NumericVisitor : IColumnValueVisitor
    {
        public int Values;
        public int Positions;
        public decimal Amounts;
        public int Nulls;

        public void Visit<T>(int columnIndex, in T value)
        {
            Values++;
            if (columnIndex == 0)
            {
                Assert.AreEqual(typeof(int), typeof(T));
                Positions += Unsafe.As<T, int>(ref Unsafe.AsRef(in value));
                return;
            }

            Assert.AreEqual(typeof(decimal?), typeof(T));
            var amount = Unsafe.As<T, decimal?>(ref Unsafe.AsRef(in value));
            Amounts += amount.GetValueOrDefault();
            if (!amount.HasValue)
                Nulls++;
        }
    }
}
