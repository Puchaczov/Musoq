using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public sealed partial class DiagnosticCore009SelectTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0143")]
    public void SingleFieldRecordDiffersFromGrouping()
    {
        const string query = "select (Id: 1), (1) from #specstressnative.rows()";
        using var compiled = CompileStructuralProjection(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        var record = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), NativeStructuralProjectionAssertions.IdRecord);
        Assert.AreEqual(StructuralTypeKind.Record, record.Kind);
        Assert.AreEqual(1, record.Fields.Count);
        Assert.AreEqual(1, record.Fields["Id"].Scalar);
        Assert.AreEqual(1, result[0].ReadValue(1));
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0145")]
    public void RecordPreservesIdAndValue()
    {
        const string query = "select (Id: 0, Value: 1) from #specstressnative.rows()";
        using var compiled = CompileStructuralProjection(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        var record = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), NativeStructuralProjectionAssertions.IdAndValueRecord);
        Assert.AreEqual(StructuralTypeKind.Record, record.Kind);
        Assert.AreEqual(2, record.Fields.Count);
        Assert.AreEqual(0, record.Fields["Id"].Scalar);
        Assert.AreEqual(1, record.Fields["Value"].Scalar);
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-SingleFieldRecordLiteralProjectsWithItsNamedField")]
    public void SingleFieldRecordLiteralProjectsWithItsNamedField()
    {
        const string query = "select (Id: 0) from #specstressnative.rows()";
        using var compiled = CompileStructuralProjection(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        var record = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), NativeStructuralProjectionAssertions.IdRecord);
        Assert.AreEqual(StructuralTypeKind.Record, record.Kind);
        Assert.AreEqual(0, record.Fields["Id"].Scalar);
    }

    [TestMethod]
    [DataRow("select (Payload: (Id: 3), Items: array { 7, 9 }, Mode: null) as Value from #specstressnative.rows()")]
    [DataRow("with projected as (select (Payload: (Id: 3), Items: array { 7, 9 }, Mode: null) as Value from #specstressnative.rows()) select Value from projected")]
    public void NestedRecordProjection_ShouldPreservePublicShape(string query)
    {
        using var compiled = CompileStructuralProjection(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(result[0].ReadValue(0)!.GetType(), result.Columns.Single().ColumnType);
        var record = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), NativeStructuralProjectionAssertions.NestedRecord);
        Assert.AreEqual(3, record.Fields.Count);
        Assert.AreEqual(3, record.Fields["Payload"].Fields["Id"].Scalar);
        Assert.AreEqual(StructuralTypeKind.Collection, record.Fields["Items"].Kind);
        Assert.AreEqual(7, record.Fields["Items"].Elements[0].Scalar);
        Assert.AreEqual(9, record.Fields["Items"].Elements[1].Scalar);
        Assert.IsNull(record.Fields["Mode"].Scalar);
    }

    private CompiledQuery CompileStructuralProjection(string query)
    {
        return InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
    }
}
