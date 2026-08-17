using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void DescQuerySample_WhenCompiledForInspection_ShouldReturnStaticQueryDescription()
    {
        var result = CompileSampleForInspection(DescQuerySampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.Contains("DescQuery", result.LogicalPlanText);
        Assert.Contains("PhysicalDescQuery", result.PhysicalPlanText);
        Assert.Contains("ReturnDesc [query Query]", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.GetQueryDescription(__columns_", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("GetRowSource<", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("GetTableByName", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("foreach", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void DescQuerySample_WhenCompiledForExecution_ShouldReturnProjectedDescription()
    {
        var table = CompileSampleForExecution(DescQuerySampleFileName).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("PersonName", table[0][0]);
        Assert.AreEqual(0, table[0][1]);
        Assert.AreEqual(typeof(string).FullName, table[0][2]);
        Assert.AreEqual("Total", table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        Assert.AreEqual(typeof(decimal).FullName, table[1][2]);
    }

    [TestMethod]
    public void DescQuerySample_WhenCheckedIn_ShouldReturnStaticQueryDescription()
    {
        var sample = ReadSample(DescQuerySampleFileName);

        Assert.Contains("desc query (select Name as PersonName, Population + Money as Total from #A.entities())", sample.Content);
        Assert.Contains("DescQuery", sample.Content);
        Assert.Contains("PhysicalDescQuery", sample.Content);
        Assert.Contains("ReturnDesc [query Query]", sample.Content);
        Assert.Contains("EvaluationHelper.GetQueryDescription(__columns_", sample.Content);
        Assert.IsFalse(sample.Content.Contains("GetRowSource<", StringComparison.Ordinal), sample.Content);
        Assert.IsFalse(sample.Content.Contains("GetTableByName", StringComparison.Ordinal), sample.Content);
        Assert.IsFalse(sample.Content.Contains("foreach", StringComparison.Ordinal), sample.Content);
    }
}
