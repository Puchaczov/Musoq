using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ApplyCarrierRegressionTests
{
    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void ChainedInterpretation_WithInjectedSample_ShouldKeepConcreteCarrier(bool parameter, bool correlated)
    {
        var prefix = parameter ? "param(batchLabel: string = 'demo') " : string.Empty;
        var argument = parameter ? "$batchLabel" : "'demo'";
        var key = correlated ? "batch.Key" : "'batch-alpha'";
        var sql = prefix + $"binary SampleReading {{ Score: int le }} select batch.Label, reading.Score from #fixture.batches({argument}, false) batch " +
            $"cross apply #fixture.samples({key}) sample cross apply Interpret<SampleReading>(sample.ReadPayload()) reading where sample.Key = 'sample-alpha'";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["batch-alpha", 73]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    [DataRow("Interpret<SampleReading>(sample.ReadPayload())", "reading.Score", 73)]
    [DataRow("TryInterpret<SampleReading>(sample.ReadPayload())", "reading.Score", 73)]
    [DataRow("PartialInterpret<SampleReading>(sample.ReadPayload())", "reading.BytesConsumed", 4)]
    [DataRow("Interpret<SampleReading>(sample.Payload)", "reading.Score", 73)]
    public void InterpretationKinds_WithChainedCarrier_ShouldEvaluateTypedArguments(string interpretation, string value, int expected)
    {
        var sql = "binary SampleReading { Score: int le } select " + value + " as V, " + value + " + 1 as Next " +
            "from #fixture.batches('demo', false) batch cross apply #fixture.samples(batch.Key) sample cross apply " + interpretation + " reading";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected, expected + 1]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    public void InterpretationArguments_ShouldRenderDirectTypedCarrierFields()
    {
        const string sql = "binary SampleReading { Score: int le } select reading.Score as N from #fixture.batches('demo', false) batch " +
            "cross apply #fixture.samples(batch.Key) sample cross apply Interpret<SampleReading>(sample.ReadPayload()) reading";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            new ApplyCarrierSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());
        Assert.Contains("ReadPayload(", inspection.GeneratedCSharpCode);
        Assert.Contains("batchsample.__rightContext", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain(".GetValue(", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ChainedCarrier_WithRepeatedProjections_ShouldPreserveValuesAcrossExecutionModes(bool parallel)
    {
        const string sql = "binary SampleReading { Score: int le } select batch.Label, reading.Score as A, reading.Score as B from #fixture.batches('demo', false) batch " +
            "cross apply #fixture.samples(batch.Key) sample cross apply Interpret<SampleReading>(sample.ReadPayload()) reading";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            new ApplyCarrierSchemaProvider(), new TestsLoggerResolver(),
            new CompilationOptions(parallelizationMode: parallel ? null : ParallelizationMode.None));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["batch-alpha", 73, 73]);
    }

    private static CompiledQuery Compile(string sql) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), new ApplyCarrierSchemaProvider(), new TestsLoggerResolver(),
        new CompilationOptions(usePrimitiveTypeValidation: false));
}
