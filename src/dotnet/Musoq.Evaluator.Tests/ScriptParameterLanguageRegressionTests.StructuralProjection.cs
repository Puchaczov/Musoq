using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterLanguageRegressionTests
{
    [TestMethod]
    public void NestedStructuralParameterProjection_ShouldPreserveNullableValues()
    {
        const string query = "param(item: (Id: int, Tags: int?[], Child: (Name: string)?, Groups: (Id: int)[][]) = " +
                             "(Id: 41, Tags: array { 1, null }, Child: null, Groups: array { array { (Id: 7) }, array {} })) " +
                             "select $item from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        var projected = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), compiled.ParameterContracts[0].StructuralType!);
        Assert.AreEqual(41, projected.Fields["Id"].Scalar);
        Assert.AreEqual(1, projected.Fields["Tags"].Elements[0].Scalar);
        Assert.IsNull(projected.Fields["Tags"].Elements[1].Scalar);
        Assert.AreEqual(StructuralTypeKind.Scalar, projected.Fields["Child"].Kind);
        Assert.IsNull(projected.Fields["Child"].Scalar);
        Assert.AreEqual(7, projected.Fields["Groups"].Elements[0].Elements[0].Fields["Id"].Scalar);
        Assert.IsEmpty(projected.Fields["Groups"].Elements[1].Elements);
    }

    [TestMethod]
    public void NestedNullableRecordCollectionProjection_ShouldPreserveNullElements()
    {
        const string query = "param(items: (Id: int)?[][] = array { array { (Id: 7), null }, array {} }) " +
                             "select $items from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        var projected = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), compiled.ParameterContracts[0].StructuralType!).Elements.Select(item => item.Elements).ToArray();
        Assert.AreEqual(2, projected.Length);
        Assert.AreEqual(2, projected[0].Count);
        Assert.AreEqual(7, projected[0][0].Fields["Id"].Scalar);
        Assert.IsNull(projected[0][1].Scalar);
        Assert.IsEmpty(projected[1]);
    }

    [TestMethod]
    public void NullableRecordParameterProjection_ShouldPreserveNullRoot()
    {
        const string query = "param(item: (Id: int)? = null) select $item from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.IsNull(result[0].ReadValue(0));
    }

    [TestMethod]
    public void NullableRecordParameterProjection_ShouldAcceptPresentHostRecord()
    {
        const string query = "param(item: (Id: int)? = null) select $item from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "item", StructuralValue.FromRecord(
            [new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar(41))]));

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(41, (NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), compiled.ParameterContracts[0].StructuralType!)).Fields["Id"].Scalar);
    }

    [TestMethod]
    [DataRow("select $item as Payload from #specstressnative.rows()")]
    [DataRow("with selected as (select $item as Payload from #specstressnative.rows()) select Payload from selected")]
    [DataRow("with selected as (select $item as Payload from #specstressnative.rows()) select a.Payload from selected a inner join selected b on 1 = 1")]
    public void StructuralParameterProjectionThroughCte_ShouldPreservePublicRecord(string projection)
    {
        var query = "param(item: (Id: int) = (Id: 41)) " + projection;
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(41, (NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), compiled.ParameterContracts[0].StructuralType!)).Fields["Id"].Scalar);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0099")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "1")]
    public void Current_P1_0099_StructuredParameterDefaultProjectsRecords()
    {
        const string query = "param(items: (Id: int)[] = array { (Id: 41), (Id: 42) }) select $items as Items from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        Assert.AreEqual(1, compiled.ParameterDefinitions.Count);
        Assert.AreEqual(1, compiled.ParameterContracts.Count);
        Assert.AreEqual(0, compiled.RequiredParameters.Count);
        var contract = compiled.ParameterContracts.Single();
        Assert.AreEqual("(Id: int)[]", contract.CanonicalTypeName);
        Assert.IsTrue(contract.IsStructured);
        Assert.IsTrue(contract.IsCollection);
        Assert.AreEqual("(Id: int)", contract.ElementCanonicalTypeName);
        Assert.AreEqual(ScriptParameterDefaultKind.Structured, contract.DefaultKind);
        var defaultItems = StructuralItems(contract.DefaultValue);
        Assert.AreEqual(41, defaultItems[0].Fields["Id"].Scalar);
        Assert.AreEqual(42, defaultItems[1].Fields["Id"].Scalar);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        var items = NativeStructuralProjectionAssertions.Read(result[0].ReadValue(0), contract.StructuralType!).Elements.ToArray();
        Assert.AreEqual(2, items.Length);
        Assert.AreEqual(41, items[0].Fields["Id"].Scalar);
        Assert.AreEqual(42, items[1].Fields["Id"].Scalar);
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-StructuralRecordScriptParameterCanBeProjected")]
    public void StructuralRecordScriptParameterCanBeProjected()
    {
        const string query = "param(item: (Id: int)) select $item from #specstressnative.rows()";
        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query,
            System.Guid.NewGuid().ToString(),
            new NativeTableCarrierSchemaProvider(),
            LoggerResolver,
            TestCompilationOptions,
            TestContext.CancellationToken);

        Assert.AreEqual(1, compiled.ParameterDefinitions.Count);
        Assert.AreEqual(1, compiled.ParameterContracts.Count);
        Assert.AreEqual(1, compiled.RequiredParameters.Count);
        var contract = compiled.ParameterContracts[0];
        Assert.AreEqual("(Id: int)", contract.CanonicalTypeName);
        Assert.IsTrue(contract.IsStructured);
        Assert.IsFalse(contract.IsCollection);
        Assert.AreEqual(Musoq.Evaluator.ScriptParameterDefaultKind.None, contract.DefaultKind);
        Assert.AreEqual(Musoq.Schema.StructuralInputs.StructuralTypeKind.Record, contract.StructuralType!.Kind);
        Assert.AreEqual("Id", contract.StructuralType.Fields[0].Name);
        Assert.AreEqual("int", contract.StructuralType.Fields[0].Type.ToCanonicalSql());

        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "item", Musoq.Schema.StructuralInputs.StructuralValue.FromRecord(
            new[]
            {
                new System.Collections.Generic.KeyValuePair<string, Musoq.Schema.StructuralInputs.StructuralValue>(
                    "Id",
                    Musoq.Schema.StructuralInputs.StructuralValue.FromScalar(41))
            }));

        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var projected = NativeStructuralProjectionAssertions.Read(table[0].ReadValue(0), compiled.ParameterContracts[0].StructuralType!);
        Assert.AreEqual(Musoq.Schema.StructuralInputs.StructuralTypeKind.Record, projected.Kind);
        Assert.AreEqual(41, projected.Fields["Id"].Scalar);
    }
    private static StructuralValue[] StructuralItems(object? value)
    {
        if (value is StructuralValue structural)
            return structural.Elements.ToArray();
        return ((System.Collections.IEnumerable)value!).Cast<StructuralValue>().ToArray();
    }
}
