using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretedSwitchArrayNullabilityTests
{
    private const string Schemas = "binary SampleValue { Score: byte } binary SampleSlot { HasValue: byte, Value: SampleValue when HasValue <> 0 } " +
        "binary SampleBatch { Slots: SampleSlot[1] } binary SampleChoice { Choice: byte, Payload: switch Choice { 7 => Primary: SampleBatch, 9 => Secondary: SampleBatch, _ => Other: byte } } ";
    private const string Source = "from values {{ (Dummy: 'x') }} d cross apply Interpret<SampleChoice>(FromHexToBytes('{0}')) v cross apply v.Payload.Primary.Slots i";

    public static IEnumerable<object[]> Cases()
    {
        yield return Case(Schemas, "0700", Source, "i.Value.Score", [null]);
        yield return Case(Schemas, "070149", Source, "i.Value.Score", [73]);
        yield return Case(Schemas.Replace("Slots: SampleSlot[1]", "SlotCount: byte, Slots: SampleSlot[SlotCount]"), "070100", Source, "i.Value.Score", [null]);
        yield return Case(Schemas.Replace("Slots: SampleSlot[1]", "SlotCount: byte, Slots: SampleSlot[SlotCount]"), "07010149", Source, "i.Value.Score", [73]);
        yield return Case(Schemas, "0900", Source.Replace("Payload.Primary", "Payload.Secondary"), "i.Value.Score", [null]);
        yield return Case(Schemas.Replace("binary SampleChoice", "binary AlternativeSlot { Flag: byte, Value: SampleValue when Flag > 0 } binary AlternativeBatch { Slots: AlternativeSlot[1] } binary SampleChoice", StringComparison.Ordinal)
            .Replace("9 => Secondary: SampleBatch", "9 => Secondary: AlternativeBatch", StringComparison.Ordinal),
            "0900", Source.Replace("Payload.Primary", "Payload.Secondary"), "i.Value.Score", [null]);
        yield return Case(Schemas, "0900", Source, "i.Value.Score", []);
        yield return Case(Schemas.Replace("binary SampleValue { Score: byte }", "binary NestedValue { Score: byte } binary SampleValue { Nested: NestedValue }"),
            "0700", Source, "i.Value.Nested.Score", [null]);
        yield return Case(Schemas.Replace("Value: SampleValue when HasValue <> 0", "Score: byte when HasValue <> 0"), "0700", Source, "i.Score", [null]);
        yield return Case("binary SampleValue { Score: byte } binary SampleSlot { HasValue: byte, Value: SampleValue when HasValue <> 0 } binary SampleBatch { Slots: SampleSlot[1] } ",
            "00", "from values {{ (Dummy: 'x') }} d cross apply Interpret<SampleBatch>(FromHexToBytes('{0}')) a cross apply a.Slots i", "i.Value.Score", [null]);
        yield return Case(Schemas.Replace("Slots: SampleSlot[1]", "SlotCount: byte, Slots: SampleSlot[SlotCount]") + "binary S { Records: SampleChoice repeat until eof } ",
            "07010007010149", "from values {{ (Dummy: 'x') }} d cross apply Interpret<S>(FromHexToBytes('{0}')) s cross apply s.Records v cross apply v.Payload.Primary.Slots i",
            "i.Value.Score", [null, 73]);
        yield return Case("binary SampleValue { Score: byte } binary SampleSlot { HasValue: byte, Value: SampleValue when HasValue <> 0 } binary SampleChoice { Choice: byte, Payload: switch Choice { 7 => Primary: SampleSlot, _ => Other: byte } } ",
            "0700", "from values {{ (Dummy: 'x') }} d cross apply Interpret<SampleChoice>(FromHexToBytes('{0}')) v", "v.Payload.Primary.Value.Score", [null]);
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void ConditionalLeaf_ShouldKeepNullableByteShape(string sql, byte?[] expected)
    {
        using var query = Compile(sql);
        using var table = query.Run();
        Assert.AreEqual(typeof(byte?), table.Columns.First().ColumnType);
        CollectionAssert.AreEqual(expected, table.Select(row => row.GetValue<byte?>(0)).ToArray());
    }

    [TestMethod]
    [DataRow("select i.Value.Score as Score {0}", "select c.Score from c c", false, false)]
    [DataRow("select i.Value.Score as Score {0}", "select c.Score from c c", false, true)]
    [DataRow("select i.Value as Value {0}", "select c.Value.Score from c c", true, false)]
    [DataRow("select i.Value as Value {0}", "select c.Value.Score from c c", true, true)]
    [DataRow("select v.Payload.Primary.Slots as Slots from values {{ (Dummy: 'x') }} d cross apply Interpret<SampleChoice>(FromHexToBytes('0700')) v", "select i.Value.Score from c c cross apply c.Slots i", true, false)]
    [DataRow("select v.Payload.Primary.Slots as Slots from values {{ (Dummy: 'x') }} d cross apply Interpret<SampleChoice>(FromHexToBytes('0700')) v", "select i.Value.Score from c c cross apply c.Slots i", true, true)]
    public void ConditionalShape_ShouldSurviveCteProjection(string projection, string final, bool complex, bool present)
    {
        var hex = present ? "070149" : "0700";
        var source = string.Format(System.Globalization.CultureInfo.InvariantCulture, Source, hex);
        projection = projection.Replace("'0700'", "'" + hex + "'", StringComparison.Ordinal);
        var sql = Schemas + "with c as (" + string.Format(System.Globalization.CultureInfo.InvariantCulture, projection, source) + ") " + final;
        using var query = Compile(sql, complex);
        using var table = query.Run();
        Assert.AreEqual(typeof(byte?), table.Columns.First().ColumnType);
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { present ? (byte?)73 : null });
    }

    [TestMethod]
    [DataRow("with c (ProjectedOpt) as (select i.Value {0}), q as (select c.ProjectedOpt as Renamed from c c) select q.Renamed.Score from q q")]
    [DataRow("with c as (select i.Value as FirstOpt {0}), q as (select c.FirstOpt as SecondOpt from c c) select q.SecondOpt.Score from q q")]
    public void NullableRecord_ShouldSurviveRenamedCteProjections(string template)
    {
        var source = string.Format(System.Globalization.CultureInfo.InvariantCulture, Source, "0700");
        var sql = Schemas + string.Format(System.Globalization.CultureInfo.InvariantCulture, template, source);
        using var query = Compile(sql, true);
        using var table = query.Run();
        Assert.AreEqual(typeof(byte?), table.Columns.First().ColumnType);
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GuardedRepeatedLeaves_ShouldStaySafeWithScalarReuse(bool reuse)
    {
        var source = string.Format(System.Globalization.CultureInfo.InvariantCulture, Source, "0700");
        var sql = Schemas + "select case when i.Value is null then null else ToInt64(i.Value.Score) + ToInt64(i.Value.Score) end as V " + source;
        var options = new CompilationOptions(useCommonSubexpressionElimination: reuse)
            .WithStabilityAwareScalarReuse(reuse).WithLoopInvariantCodeMotion(reuse);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), new TestSchemaProvider(),
            new TestsLoggerResolver(), options);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }

    [TestMethod]
    public void RecordProjection_ShouldKeepPrimitiveOutputValidationWhenEnabled()
    {
        var source = string.Format(System.Globalization.CultureInfo.InvariantCulture, Source, "0700");
        var sql = Schemas + "with c as (select i.Value as Value " + source + ") select c.Value.Score from c c";
        var error = Assert.ThrowsExactly<MusoqQueryException>(() => Compile(sql));
        Assert.Contains("MQ3027", error.Message);
    }

    [TestMethod]
    public void NullableSwitchElement_ShouldUseConcreteNullSafeGeneratedAccess()
    {
        var sql = (string)Case(Schemas, "0700", Source, "i.Value.Score", [null])[0];
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), new TestSchemaProvider(),
            new TestsLoggerResolver(), new CompilationOptions());
        Assert.Contains("ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.SampleSlot>", inspection.GeneratedCSharpCode);
        Assert.Contains("?.Score", inspection.GeneratedCSharpCode);
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    private static object[] Case(string schemas, string hex, string source, string expression, byte?[] expected) =>
        [schemas + "select " + expression + " " + string.Format(System.Globalization.CultureInfo.InvariantCulture, source, hex), expected];

    private static CompiledQuery Compile(string sql, bool complex = false) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), new TestSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: !complex));

}
