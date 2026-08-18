namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class QueryRowBenchmarkCorrectnessTests
{
    [TestMethod]
    [DataRow(2)]
    [DataRow(8)]
    [DataRow(32)]
    [DataRow(64)]
    public void SourceMatrix_WhenSetupCompletes_ShouldKeepEveryModeEquivalent(int fieldCount)
    {
        var benchmark = new QueryScopedSourceMaterializationBenchmark { FieldCount = fieldCount };
        try
        {
            benchmark.Setup();

            Assert.AreEqual(benchmark.LegacyRows(), benchmark.QueryScopedStructRows());
            Assert.AreEqual(benchmark.LegacyRows(), benchmark.QueryScopedClassRows());
            Assert.AreEqual(benchmark.LegacySelectiveProjection(), benchmark.QueryScopedSelectiveProjection());
            Assert.AreEqual(benchmark.LegacySelectiveProjection(), benchmark.QueryScopedClassSelectiveProjection());
            Assert.AreEqual(benchmark.LegacyHighRejection(), benchmark.QueryScopedHighRejection());
            Assert.AreEqual(benchmark.LegacyHighRejection(), benchmark.QueryScopedClassHighRejection());
            Assert.AreEqual(benchmark.LegacyAggregation(), benchmark.QueryScopedStructAggregation());
            Assert.AreEqual(benchmark.LegacyAggregation(), benchmark.QueryScopedClassAggregation());
            Assert.AreEqual(benchmark.LegacyEarlyTake(), benchmark.QueryScopedEarlyTake());
            Assert.AreEqual(benchmark.LegacyEarlyTake(), benchmark.QueryScopedClassEarlyTake());
            Assert.AreEqual(benchmark.LegacyNumericRows(), benchmark.QueryScopedNumericStructRows());
            Assert.AreEqual(benchmark.LegacyNumericRows(), benchmark.QueryScopedNumericClassRows());
            Assert.AreEqual(
                benchmark.LegacyObjectArrayMaterialization(),
                benchmark.QueryScopedStructMaterialization());
            Assert.AreEqual(
                benchmark.LegacyObjectArrayMaterialization(),
                benchmark.QueryScopedClassMaterialization());
            Assert.AreEqual(
                benchmark.LegacyNumericObjectArrayMaterialization(),
                benchmark.QueryScopedNumericStructMaterialization());
            Assert.AreEqual(
                benchmark.LegacyNumericObjectArrayMaterialization(),
                benchmark.QueryScopedNumericClassMaterialization());
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    [TestMethod]
    [DataRow(QueryRowCompiledScenario.NullableNumeric2Full)]
    [DataRow(QueryRowCompiledScenario.NullableNumeric8Full)]
    [DataRow(QueryRowCompiledScenario.NullableNumeric32Full)]
    [DataRow(QueryRowCompiledScenario.NullableNumeric64Full)]
    [DataRow(QueryRowCompiledScenario.NullableString8Full)]
    [DataRow(QueryRowCompiledScenario.NullableNumeric8Selective)]
    [DataRow(QueryRowCompiledScenario.NullableString8HighRejection)]
    [DataRow(QueryRowCompiledScenario.NullableNumeric8Aggregation)]
    [DataRow(QueryRowCompiledScenario.NullableNumeric8EarlyTake)]
    public void CompiledMatrix_WhenSetupCompletes_ShouldKeepWarmModesEquivalent(
        QueryRowCompiledScenario scenario)
    {
        var benchmark = new QueryScopedCompiledExecutionBenchmark { Scenario = scenario };
        try
        {
            benchmark.Setup();

            Assert.AreEqual(benchmark.LegacyWarmExecution(), benchmark.QueryScopedWarmExecution());
        }
        finally
        {
            benchmark.Cleanup();
        }
    }
}
