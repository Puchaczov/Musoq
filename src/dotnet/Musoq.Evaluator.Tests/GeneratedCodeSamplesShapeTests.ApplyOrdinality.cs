using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCompiledForInspection_ShouldUseIndexedLoopWithoutLinqProjection()
    {
        var result = CompileSampleForInspection(ApplyWithOrdinalitySampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.Contains("ForEachWithOrdinality [nOrdinal, n in", result.ExecutionPlanText);
        AssertDirectScalarOrdinality(result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(".Select((", StringComparison.Ordinal), result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("Select((n, nOrdinal)", StringComparison.Ordinal), result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ApplyWithOrdinalitySampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Number", typeof(int)),
            ("NumberOrdinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", 1, 0],
            ["left", 2, 1],
            ["right", 3, 0]);
    }

    [TestMethod]
    public void ApplyWithOrdinalitySample_WhenCheckedIn_ShouldUseIndexedLoopWithoutLinqProjection()
    {
        var sample = ReadSample(ApplyWithOrdinalitySampleFileName);

        AssertDirectScalarOrdinality(sample.Content);
        Assert.IsFalse(sample.Content.Contains(".Select((", StringComparison.Ordinal), sample.Content);
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.SmartForEach(", StringComparison.Ordinal), sample.Content);
    }

    private static void AssertDirectScalarOrdinality(string code)
    {
        Assert.Contains("is int[] nOrdinalArray", code);
        Assert.Contains("int n = nOrdinalArray[nOrdinal];", code);
        Assert.Contains("is List<int> nOrdinalConcreteList", code);
        Assert.Contains("foreach (var n in nOrdinalRows)", code);
        Assert.DoesNotContain("ConvertScalarEnumerableToTypedChunks<int>", code);
        Assert.DoesNotContain("ConvertEnumerableOutputToChunks<int>", code);
    }
}
