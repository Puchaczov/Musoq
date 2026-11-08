using System;
using System.Collections.Generic;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterLanguageRegressionTests
{
    [TestMethod]
    [DataRow(0, 7)]
    [DataRow(1, 9)]
    [DataRow(-1, 9)]
    [DataRow(-5, 9)]
    [DataRow(2, null)]
    public void CollectionParameterIndex_ShouldPreserveExistingBounds(int index, int? expected)
    {
        const string query = "param(values: int?[], position: int) select $values[$position] as Selected from #specstressnative.rows()";
        using var compiled = CompileCollectionIndex(query);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", new List<int?> { 7, 9 }.AsReadOnly());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "position", index);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Selected", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [expected]);
    }

    [TestMethod]
    public void EmptyNonnullableCollectionIndex_ShouldReturnElementDefault()
    {
        using var compiled = CompileCollectionIndex("param(values: int[]) select $values[0] as Selected from #specstressnative.rows()");
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", new List<int>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Selected", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [0]);
    }

    [TestMethod]
    public void NullCollectionIndex_ShouldReturnNullableElementDefault()
    {
        using var compiled = CompileCollectionIndex("param(values: int?[]? = null) select $values[0] as Selected from #specstressnative.rows()");
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Selected", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { null });
    }

    [TestMethod]
    public void CollectionLetIndex_ShouldRetainDeclaredElementType()
    {
        using var compiled = CompileCollectionIndex("let values: int?[] = array { 7, null }; select $values[0] as Selected from #specstressnative.rows()");
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Selected", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [7]);
    }

    [TestMethod]
    [DataRow("param(value: int) select $value[0] as Selected from #specstressnative.rows()")]
    [DataRow("param(value: int[]) select $value['first'] as Selected from #specstressnative.rows()")]
    public void InvalidParameterIndex_ShouldReportIndexerDiagnostic(string query)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CompileCollectionIndex(query));
        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, exception.PrimaryEnvelope.Code);
    }

    private CompiledQuery CompileCollectionIndex(string query) => InstanceCreator.CompileForExecution(
        query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
        LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0098")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "1")]
    public void Current_P1_0098_NullableCollectionElementsPreserveNull()
    {
        const string query = "param(values: int?[]) select $values[1] as SecondValue from #specstressnative.rows()";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        Assert.AreEqual(1, compiled.ParameterDefinitions.Count);
        Assert.AreEqual(1, compiled.RequiredParameters.Count);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", new int?[] { 7, null });
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("SecondValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { null });
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "values", new int?[] { 7, 9 });
        using var populated = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(populated, new object?[] { 9 });
    }
}
