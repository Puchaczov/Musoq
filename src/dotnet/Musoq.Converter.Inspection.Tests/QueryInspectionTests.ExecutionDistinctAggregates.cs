using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenGroupedCountDistinctUsesTypedMergeableKernels_ShouldEmitParallelCode()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, Count(distinct d.Dummy) as DistinctNames, Count(distinct Length(d.Dummy)) as DistinctLengths from #system.dual() d group by d.Dummy",
            new CompilationOptions(ParallelizationMode.Full));

        AssertUsesExecutionBackend(result);
        AssertTypedSingleKeyAggregateContext(result.ExecutionPlanText);
        AssertExecutionPlanContains("ParallelSingleKeyAggregateLoop", result.ExecutionPlanText);
        AssertGeneratedCSharpContains(
            "CountDistinctReferenceAggregateKernel<string>.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "CountDistinctReferenceAggregateKernel<string>.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "CountDistinctNullableAggregateKernel<int>.Set(ref group.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains(
            "CountDistinctNullableAggregateKernel<int>.Get(in finalGroup.__agg",
            result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("CountDistinctReferenceAggregateKernel<string>.Merge", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("CountDistinctNullableAggregateKernel<int>.Merge", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("public void MergeFrom(ResultAggregateGroup source)", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("mergedGroupRef.MergeFrom(sourceGroup)", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("HashSet<object>"));
        AssertNoLegacyAggregateRuntime(result.GeneratedCSharpCode);
    }
}
