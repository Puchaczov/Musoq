using System;
using System.Collections.Generic;
using Musoq.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{
    [TestMethod]
    [TestProperty("CandidateId", "P1-045")]
    public void IntersectIdenticalLeftRows_ShouldReturnOneMatchingRow()
    {
        const string query = "select Name from values { (Name: 'A'), (Name: 'B'), (Name: 'B') } leftSource intersect select Name from values { (Name: 'B'), (Name: 'C') } rightSource";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["B"]);
    }
    [TestMethod]
    public void IntersectSubsetKey_ShouldRetainDistinctNonKeyValues()
    {
        const string query = "select Id, Name from values { (Id: 1, Name: 'first'), (Id: 1, Name: 'first'), (Id: 1, Name: 'second') } l intersect (Id) select OtherId as Id, OtherName as Name from values { (OtherId: 1, OtherName: 'right'), (OtherId: 1, OtherName: 'right') } r";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, "first"], [1, "second"]);
    }

    [TestMethod]
    public void IntersectNullableRows_ShouldRemoveIdenticalNulls()
    {
        const string query = "select NullableValue from #A.Entities() intersect select NullableValue from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { NullableValue = 1 }, new BasicEntity { NullableValue = null }, new BasicEntity { NullableValue = null }],
            ["#B"] = [new BasicEntity { NullableValue = null }, new BasicEntity { NullableValue = null }]
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { null });
    }

    [TestMethod]
    [DataRow("intersect", 1)]
    [DataRow("intersect (Id)", 1)]
    public void IntersectDoubleOutput_ShouldUseGeneratedEqualityForDuplicateRemoval(string operation, int expectedCount)
    {
        var query = $"select Id, Value::double as Value from values {{ (Id: 1, Value: 2.0d), (Id: 1, Value: 2.0d) }} l {operation} select Id, Value::double as Value from values {{ (Id: 1, Value: 2.0d) }} r";
        var provider = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>());
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(), provider,
            LoggerResolver, TestCompilationOptions);
        Assert.Contains("GeneratedEqualityLoop", inspection.PlanningText);
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(expectedCount, result.Count);
        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, 2d]);
    }

    [TestMethod]
    public void IntersectDisjointRows_ShouldReturnEmptyResult()
    {
        const string query = "select Name from values { (Name: 'left'), (Name: 'left') } l intersect select Name from values { (Name: 'right') } r";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    [DataRow("intersect", 0)]
    [DataRow("intersect (Id)", 2)]
    public void IntersectNaN_ShouldPreserveExistingEquality(string operation, int expectedCount)
    {
        var query = $"param(value: double) select Id, Value from values {{ (Id: 1, Value: $value), (Id: 1, Value: $value) }} l {operation} select Id, Value from values {{ (Id: 1, Value: $value) }} r";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "value", double.NaN);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(expectedCount, result.Count);
    }

}
