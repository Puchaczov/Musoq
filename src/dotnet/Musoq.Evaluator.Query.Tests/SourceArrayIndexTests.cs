using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourceArrayIndexTests
{
    [TestMethod]
    public void SourceArrayIndex_ShouldRetainNullableElementMetadataAndValues()
    {
        using var query = Compile("select r.OptionalItems[r.Position] as Value from #schema.first() r", new([], 1));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    [DataRow("Items[Position]", 1, 20)]
    [DataRow("r.Items[r.Position + 1]", 0, 20)]
    [DataRow("r.Items[r.Position]", -1, 30)]
    [DataRow("r.Items[r.Position]", -4, 30)]
    [DataRow("r.Items[r.Position]", int.MinValue, 20)]
    [DataRow("r.Items[r.Position]", 10, 0)]
    [DataRow("r.Items[0]", 1, 10)]
    [DataRow("r.Records[r.Position].Value", 1, 20)]
    [DataRow("r.Records[r.Position].Value", 99, 0)]
    [DataRow("r.Records[r.Position].Values[r.Position - 1]", 1, 300)]
    [DataRow("r.Records[r.Position].Values[1]", 1, 400)]
    public void SourceArrayIndex_ShouldUseTypedSafeAccess(string expression, int index, int expected)
    {
        var sql = $"select {expression} as Value from #schema.first() r";
        using var query = Compile(sql, new([10, 20, 30], index));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
    }

    [TestMethod]
    [DataRow(new int[] { 10, 20 }, 0, 10)]
    [DataRow(new int[] { 10, 20 }, -1, 20)]
    [DataRow(new int[] { 10, 20 }, 10, 0)]
    [DataRow(new int[] { }, 0, 0)]
    [DataRow(null, -1, 0)]
    public void SourceArrayIndex_ShouldEvaluateReceiverThenIndexOnce(int[]? items, int index, int expected)
    {
        using var fixture = new ArrayIndexEvaluationFixture();
        using var query = Compile(
            $"select MarkItems('{fixture.Key}', r.Items)[MarkIndex('{fixture.Key}', r.Position)] as Value from #schema.first() r",
            new(items, index)
        );
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
        CollectionAssert.AreEqual(new[] { "receiver", "index" }, fixture.Evaluations);
    }

    [TestMethod]
    [DataRow("params(i: int = 1) select r.Items[$i] as Value from #schema.first() r")]
    [DataRow(
        "with c as (select r.Items, r.Position from #schema.first() r) select c.Items[c.Position] as Value from c c"
    )]
    public void SourceArrayIndex_ShouldRetainParametersAndCteReceivers(string sql)
    {
        using var query = Compile(sql, new([10, 20, 30], 1));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [20]);
    }

    [TestMethod]
    [DataRow("r.Items[r.Position + 0.5]")]
    [DataRow("r.Items['bad']")]
    public void InvalidSourceArrayIndex_ShouldReportAnIndexerBindingDiagnostic(string expression)
    {
        var error = Assert.Throws<MusoqQueryException>(() =>
            Compile($"select {expression} from #schema.first() r", new([10], 0))
        );
        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, error.PrimaryEnvelope!.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, error.PrimaryEnvelope.Phase);
    }

    private static CompiledQuery Compile(string sql, ArrayIndexEvaluationFixture.Input input) =>
        GeneratedExecutionIlInspection.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider<
                ArrayIndexEvaluationFixture.Input,
                ArrayIndexEvaluationFixture.Functions
            >([input]),
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
}
