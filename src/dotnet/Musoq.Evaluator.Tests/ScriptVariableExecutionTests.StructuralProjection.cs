using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public sealed partial class ScriptVariableExecutionTests
{
    [TestMethod]
    public void StructuralLetProjectionThroughRepeatedCte_ShouldPreservePublicRecord()
    {
        const string query = "let entry = (Id: 41); " +
                             "with selected as (select $entry as Payload from #specstressnative.rows()) " +
                             "select a.Payload from selected a inner join selected b on 1 = 1";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, new CompilationOptions(usePrimitiveTypeValidation: false), TestContext.CancellationToken);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(41, (NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), NativeStructuralProjectionAssertions.IdRecord)).Fields["Id"].Scalar);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0133")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "1")]
    public void Current_P1_0133_StructuralLetPreservesFieldPresence()
    {
        const string query = "let entries = array { (Id: 1, Mode: 'literal'), (Id: 2), (Id: 3, Mode: null) }; select $entries from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, new CompilationOptions(usePrimitiveTypeValidation: false), TestContext.CancellationToken);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        var items = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), NativeStructuralProjectionAssertions.RecordsWithOptionalMode).Elements.ToArray();
        Assert.AreEqual(3, items.Length);
        Assert.AreEqual(1, items[0].Fields["Id"].Scalar);
        Assert.AreEqual("literal", items[0].Fields["Mode"].Scalar);
        Assert.AreEqual(2, items[1].Fields["Id"].Scalar);
        Assert.IsFalse(items[1].Fields.ContainsKey("Mode"));
        Assert.AreEqual(3, items[2].Fields["Id"].Scalar);
        Assert.IsTrue(items[2].Fields.ContainsKey("Mode"));
        Assert.IsNull(items[2].Fields["Mode"].Scalar);
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-SingleFieldStructuralLetValueCanBeProjected")]
    public void SingleFieldStructuralLetValueCanBeProjected()
    {
        const string query = "let entry = (Id: 1); select $entry from #specstressnative.rows()";
        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query,
            System.Guid.NewGuid().ToString(),
            new NativeTableCarrierSchemaProvider(),
            LoggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false),
            TestContext.CancellationToken);

        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var projected = NativeStructuralProjectionAssertions.Read(table[0].ReadValue(0), NativeStructuralProjectionAssertions.IdRecord);
        Assert.AreEqual(Musoq.Schema.StructuralInputs.StructuralTypeKind.Record, projected.Kind);
        Assert.AreEqual(1, projected.Fields["Id"].Scalar);
    }
}
