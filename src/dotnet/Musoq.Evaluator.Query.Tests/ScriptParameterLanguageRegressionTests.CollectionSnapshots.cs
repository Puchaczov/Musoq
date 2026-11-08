using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterLanguageRegressionTests
{
    [TestMethod]
    [DataRow("array", "param(values: int[])")]
    [DataRow("readOnly", "param(values: int[])")]
    [DataRow("array", "param(values: int[], item: (Id: int) = (Id: 1))")]
    [DataRow("readOnly", "param(values: int[], item: (Id: int) = (Id: 1))")]
    public void CollectionSnapshot_ShouldPrecedeSourceOpeningAndRefreshEachRun(string hostKind, string declaration)
    {
        var query = declaration + " select $values from #specstressnative.rows()";
        var values = new[] { 7, 9 };
        var sourceOpened = false;
        var provider = new NativeTableCarrierSchemaProvider(sourceOpened: () =>
        {
            values[0] = 70;
            values[1] = 90;
            sourceOpened = true;
        });
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), provider,
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", CreateCollectionHost(hostKind, values));

        using var first = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, first.Count);
        Assert.IsTrue(sourceOpened);
        CollectionAssert.AreEqual(new[] { 70, 90 }, values);
        CollectionAssert.AreEqual(new[] { 7, 9 }, ((IEnumerable<int>)first[0].ReadValue(0)!).ToArray());

        using var second = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, second.Count);
        CollectionAssert.AreEqual(new[] { 70, 90 }, ((IEnumerable<int>)second[0].ReadValue(0)!).ToArray());
        CollectionAssert.AreEqual(new[] { 7, 9 }, ((IEnumerable<int>)first[0].ReadValue(0)!).ToArray());
    }

    private static IReadOnlyList<int> CreateCollectionHost(string hostKind, int[] values) => hostKind switch
    {
        "array" => values,
        "readOnly" => Array.AsReadOnly(values),
        _ => throw new ArgumentOutOfRangeException(nameof(hostKind))
    };

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0101")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "1")]
    public void Current_P1_0101_HostCollectionIsSnapshotted()
    {
        const string query = "param(values: int[]) select $values as ProjectedValues from #specstressnative.rows()";
        var values = new List<int> { 7, 9 };
        var callbackRan = false;
        var provider = new NativeTableCarrierSchemaProvider(beforeRows: () =>
        {
            values[0] = 70;
            values[1] = 90;
            callbackRan = true;
        });
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), provider,
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        Assert.AreEqual(1, compiled.ParameterDefinitions.Count);
        Assert.AreEqual(1, compiled.RequiredParameters.Count);
        var contract = compiled.ParameterContracts.Single();
        Assert.AreEqual("int[]", contract.CanonicalTypeName);
        Assert.IsTrue(contract.IsCollection);
        Assert.AreEqual(typeof(int), contract.ElementClrType);
        Assert.AreEqual("int", contract.ElementCanonicalTypeName);
        Assert.IsFalse(contract.HasDefaultValue);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", values);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(callbackRan);
        CollectionAssert.AreEqual(new[] { 70, 90 }, values.ToArray());
        CollectionAssert.AreEqual(new object[] { 7, 9 }, ((System.Collections.IEnumerable)result[0].ReadValue(0)!).Cast<object>().ToArray());
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-HostCollectionParameterIsSnapshottedBeforeSourceMaterialization")]
    public void HostCollectionParameterIsSnapshottedBeforeSourceMaterialization()
    {
        const string query = "param(values: int[]) select $values from #specstressnative.rows()";
        var hostValues = new System.Collections.Generic.List<int> { 1, 2 };
        var mutationAppliedDuringSourceRead = false;
        var provider = new global::Musoq.Evaluator.Tests.NativeTableCarrierSchemaProvider(() =>
        {
            hostValues[0] = 70;
            hostValues[1] = 90;
            mutationAppliedDuringSourceRead = true;
        });
        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query,
            System.Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions,
            TestContext.CancellationToken);

        Assert.AreEqual(1, compiled.ParameterDefinitions.Count);
        var contract = compiled.ParameterContracts[0];
        Assert.AreEqual("int[]", contract.CanonicalTypeName);
        Assert.IsTrue(contract.IsCollection);
        Assert.IsFalse(contract.IsStructured);

        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", hostValues);
        hostValues[0] = 7;
        hostValues[1] = 9;

        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);
        var projectedValues = System.Linq.Enumerable.ToArray(
            (System.Collections.Generic.IEnumerable<int>)table[0].ReadValues()[0]!);

        Assert.IsTrue(mutationAppliedDuringSourceRead);
        CollectionAssert.AreEqual(new[] { 70, 90 }, hostValues);
        CollectionAssert.AreEqual(new[] { 7, 9 }, projectedValues);
    }
}
