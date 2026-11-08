using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretedCteShapeTests
{
    private const string Schemas = "binary Inner { X: byte, Y: byte } binary Outer { Inner: Inner } ";
    private const string Source = "from #test.entities() f cross apply Interpret<Outer>(f.Content) s";

    [TestMethod]
    [DataRow("with c as (select s.Inner as Inner ", ") select c.Inner.X as X, c.Inner.Y as Y from c c")]
    [DataRow("with c (Renamed) as (select s.Inner as Inner ", ") select c.Renamed.X as X, c.Renamed.Y as Y from c c")]
    [DataRow("with c as (select s as Record ", ") select c.Record.X as X, c.Record.Y as Y from c c")]
    [DataRow("with c as (select s.Inner as Inner ", "), next as (select c.Inner as Renamed from c c) select n.Renamed.X as X, n.Renamed.Y as Y from next n")]
    public void DecodedRecordThroughCte_ShouldPreserveNestedConcreteFields(string before, string after)
    {
        using var query = Compile(Schemas + before + Source + after);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertColumns(table, ("X", typeof(byte)), ("Y", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, (byte)2]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    public void PrimitiveProjectionThroughCte_ShouldRetainDefaultValidation()
    {
        using var query = Compile(Schemas + "with c as (select s.Inner.X as X " + Source + ") select c.X from c c", true);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1]);
    }

    [TestMethod]
    public void DecodedRecordProjection_WithStrictPrimitiveValidation_ShouldRemainRejected()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => Compile(Schemas + "with c as (select s.Inner as Inner " + Source +
            ") select c.Inner.X from c c", true));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3027_InvalidExpressionType, exception.PrimaryEnvelope!.Code);
    }

    [TestMethod]
    [DataRow("c", "Items")]
    [DataRow("c (Renamed)", "Renamed")]
    public void DecodedArrayThroughCte_ShouldPreserveElementSchema(string declaration, string column)
    {
        var sql = "binary Inner { X: byte, Y: byte } binary Outer { Items: Inner[2] } " +
            $"with {declaration} as (select s.Items as Items {Source}) " +
            $"select i.X, i.Y from c c cross apply c.{column} i";
        using var query = Compile(sql, bytes: [1, 2, 3, 4]);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, (byte)2], [(byte)3, (byte)4]);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public void DecodedIndexedElementThroughCte_ShouldPreserveItsRecordSchema(int index)
    {
        var sql = "binary Inner { X: byte, Y: byte } binary Outer { Items: Inner[2] } " +
            $"with c as (select s.Items[{index}] as Item {Source}) select c.Item.X as X, c.Item.Y as Y from c c";
        using var query = Compile(sql, bytes: [1, 2, 3, 4]);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)3, (byte)4]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    [DataRow("0002", null)]
    [DataRow("010102", (byte)1)]
    public void DecodedNullableFieldThroughCte_ShouldRetainItsNullableType(string hex, byte? expected)
    {
        var sql = "binary Inner { Flag: byte, X: byte when Flag = 1, Y: byte } binary Outer { Inner: Inner } " +
            $"with c as (select s.Inner as Inner {Source}) select c.Inner.X as X, c.Inner.Y as Y from c c";
        using var query = Compile(sql, bytes: Convert.FromHexString(hex));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertColumns(table, ("X", typeof(byte?)), ("Y", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected, (byte)2]);
    }

    [TestMethod]
    public void DecodedCteMetadata_WhenReexecuted_ShouldRetainConcreteTypes()
    {
        using var query = Compile(Schemas + "with c as (select s.Inner as Inner " + Source +
            ") select c.Inner.X as X, c.Inner.Y as Y from c c");
        using var first = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(first, [(byte)1, (byte)2]);
        using var second = query.Run();
        TableMaterializationTestHelper.AssertColumns(second, ("X", typeof(byte)), ("Y", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(second, [(byte)1, (byte)2]);
    }

    [TestMethod]
    [DataRow("byte", 1, typeof(byte))]
    [DataRow("short le", 513, typeof(short))]
    public void DecodedCteMetadata_WithSameSchemaNames_ShouldKeepEachDefinitionsTypes(string type, int expected, Type clrType)
    {
        var sql = $"binary Inner {{ X: {type} }} binary Outer {{ Inner: Inner }} " +
            $"with c as (select s.Inner as Inner {Source}) select c.Inner.X as X from c c";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertColumns(table, ("X", clrType));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [Convert.ChangeType(expected, clrType)]);
    }

    [TestMethod]
    public void DecodedCte_WhenLoadedFromArtifact_ShouldRetainConcreteNestedValues()
    {
        var sql = Schemas + "with c as (select s.Inner as Inner " + Source +
            ") select c.Inner.X as X, c.Inner.Y as Y from c c";
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var provider = CreateProvider([1, 2]);
        var logger = new TestsLoggerResolver();
        var artifact = InstanceCreator.CompileArtifactWithDiagnostics(sql, Guid.NewGuid().ToString(), provider, logger, options);
        Assert.IsTrue(artifact.Succeeded, string.Join("\n", artifact.Diagnostics));
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(sql, artifact.Artifact!, provider, logger, options);
        Assert.IsTrue(loaded.Succeeded, string.Join("\n", loaded.Diagnostics));
        using var query = loaded.CompiledQuery!;
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, (byte)2]);
    }

    private static CompiledQuery Compile(string sql, bool strict = false, byte[]? bytes = null) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), CreateProvider(bytes ?? [1, 2]), new TestsLoggerResolver(),
        new CompilationOptions(usePrimitiveTypeValidation: strict));

    private static ISchemaProvider CreateProvider(byte[] bytes) => new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
        {
            ["#test"] = [new BinaryEntity { Content = bytes }]
        });
}
