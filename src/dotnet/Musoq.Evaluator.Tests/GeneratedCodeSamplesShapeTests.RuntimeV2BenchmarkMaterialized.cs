using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RuntimeV2BenchmarkMaterializedSamples_WhenCheckedIn_ShouldExposeSlowerBenchmarkShapes()
    {
        var samples = ReadNamedSamples(BenchmarkMaterializedSampleFileNames)
            .ToDictionary(static item => item.FileName, static item => item.Content);
        var cseNoDuplicate = samples[BenchmarkCseNoDuplicateMaterializedSampleFileName];
        var cseCaseNoDuplicate = samples[BenchmarkCseCaseNoDuplicateMaterializedSampleFileName];
        var parallelTableAdd = samples[BenchmarkParallelTableAddMaterializedSampleFileName];
        var heavyMixed = samples[BenchmarkOptimizedHeavyMixedMaterializedSampleFileName];
        var mixedColumnMethod = samples[BenchmarkOptimizedMixedColumnMethodMaterializedSampleFileName];
        var compilationSimple = samples[BenchmarkCompilationSimpleMaterializedSampleFileName];
        var compilationComplex = samples[BenchmarkCompilationComplexMaterializedSampleFileName];

        foreach (var sample in new[]
                 {
                     cseNoDuplicate,
                     cseCaseNoDuplicate,
                     parallelTableAdd,
                     heavyMixed,
                     mixedColumnMethod,
                     compilationSimple
                 })
        {
            Assert.Contains("var __musoqMaterializedTable = QueryRows.DeferredTable", sample);
            Assert.Contains("_ = __musoqMaterializedTable.Count;", sample);
        }

        Assert.Contains("SELECT Value * 2, Name", cseNoDuplicate);
        Assert.Contains("__resultBenchmarkParityLibrary0.ExpensiveMethod(ko3iko.Value)", cseNoDuplicate);
        Assert.IsFalse(cseNoDuplicate.Contains("GetOrAddCachedMethod", StringComparison.Ordinal));

        Assert.Contains("CASE WHEN ExpensiveMethod(Value) > 200", cseCaseNoDuplicate);
        Assert.Contains("__resultBenchmarkParityLibrary0.ExpensiveMethod(ko3iko.Value)", cseCaseNoDuplicate);
        Assert.IsFalse(cseCaseNoDuplicate.Contains("GetOrAddCachedMethod", StringComparison.Ordinal));

        Assert.Contains("HeavyComputation(Value) as Heavy", parallelTableAdd);
        Assert.Contains(ParallelFilterProjectLoopPattern, parallelTableAdd);
        Assert.Contains(ParallelProjectionRowsPattern, parallelTableAdd);
        Assert.Contains(TableParallelProjectRowsPattern, parallelTableAdd);

        Assert.Contains("ExpensiveCompute(Value) * 2", heavyMixed);
        Assert.Contains("StringTransform(name)", heavyMixed);

        Assert.Contains("Name + '_' + StringTransform(Name)", mixedColumnMethod);
        Assert.IsFalse(mixedColumnMethod.Contains("System.Collections.Concurrent.ConcurrentDictionary<int, decimal>", StringComparison.Ordinal));
        Assert.Contains("StringTransform(name)", mixedColumnMethod);
        Assert.Contains("QueryRows.FromRowShards(", mixedColumnMethod);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<", mixedColumnMethod);
        Assert.DoesNotContain("TableProjectionRows.ProjectOptionalRowsSerial<", mixedColumnMethod);
        Assert.IsFalse(mixedColumnMethod.Contains("TypedProjectionRows.ProjectOptionalValuesParallel<", StringComparison.Ordinal));
        Assert.IsFalse(mixedColumnMethod.Contains("private IEnumerable<ResultShape0> ComputeShapeRows", StringComparison.Ordinal));

        Assert.Contains("SELECT City, Country, Population", compilationSimple);
        Assert.Contains("ORDER BY Population desc", compilationComplex);
        Assert.Contains("GROUP BY City, Country, Population", compilationComplex);
    }

    [TestMethod]
    public void InterpretationBenchmarkMaterializedSamples_WhenCheckedIn_ShouldExposeScalarInterpretationShape()
    {
        var samples = ReadNamedSamples(BenchmarkInterpretationMaterializedSampleFileNames)
            .ToDictionary(static item => item.FileName, static item => item.Content);
        var multipleFiles = samples[BenchmarkInterpretationMultipleFilesMaterializedSampleFileName];
        var highThroughput = samples[BenchmarkInterpretationHighThroughputMaterializedSampleFileName];

        foreach (var sample in BenchmarkInterpretationMaterializedSampleFileNames.Select(fileName => samples[fileName]))
        {
            Assert.Contains("var __musoqMaterializedTable = QueryRows.DeferredTable", sample);
            Assert.Contains("_ = __musoqMaterializedTable.Count;", sample);
            Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", sample);
            Assert.Contains("InterpretSource [", sample);
            Assert.Contains("ScalarForEach [", sample);
            Assert.IsFalse(sample.Contains("EvaluationHelper.WrapScalarForCrossApply", StringComparison.Ordinal));
            Assert.IsFalse(sample.Contains("EvaluationHelper.ConvertEnumerableOutputToChunks", StringComparison.Ordinal));
            Assert.Contains("ReturnDeferredTable [result: ResultRow0 <- ResultShape0]", sample);
        }

        Assert.Contains("binary SimpleHeader", multipleFiles);
        Assert.Contains("new Musoq.Generated.Interpreters.SimpleHeader()", multipleFiles);
        Assert.AreEqual(3, CountOccurrences(multipleFiles, "new Musoq.Generated.Interpreters.SimpleHeader()"));
        Assert.AreEqual(0, CountOccurrences(multipleFiles, "EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.SimpleHeader>"));

        Assert.Contains("binary TinyHeader", highThroughput);
        Assert.Contains("new Musoq.Generated.Interpreters.TinyHeader()", highThroughput);
        Assert.AreEqual(3, CountOccurrences(highThroughput, "new Musoq.Generated.Interpreters.TinyHeader()"));
        Assert.AreEqual(0, CountOccurrences(highThroughput, "EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.TinyHeader>"));
    }

    [TestMethod]
    public void OptimizedMixedColumnBenchmarkSample_WhenCheckedIn_ShouldExposeRowLocalMethodCseWithoutSharedCache()
    {
        var sample = ReadSample(BenchmarkOptimizedMixedColumnMethodMaterializedSampleFileName)
            .Content;

        Assert.Contains("Name + '_' + StringTransform(Name)", sample);
        Assert.Contains("ParallelFilterProjectLoop", sample);
        Assert.IsFalse(sample.Contains("System.Collections.Concurrent.ConcurrentDictionary<int, decimal>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.GetOrAddCachedMethod<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityLibrary, int, decimal>", StringComparison.Ordinal));
        Assert.Contains("StringTransform(name)", sample);
        Assert.Contains("decimal expensiveCompute = (decimal)__resultBenchmarkParityLibrary0.ExpensiveCompute(value);", sample);
        Assert.Contains("expensiveCompute > 50", sample);
        Assert.Contains("QueryRows.FromRowShards(", sample);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<", sample);
        Assert.DoesNotContain("TableProjectionRows.ProjectOptionalRowsSerial<", sample);
        Assert.IsFalse(sample.Contains("__musoqFinalShapeRows", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("private IEnumerable<ResultShape0> ComputeShapeRows", StringComparison.Ordinal));
    }
}
