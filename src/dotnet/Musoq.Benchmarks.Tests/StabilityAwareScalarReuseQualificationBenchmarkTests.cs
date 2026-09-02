namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class StabilityAwareScalarReuseQualificationBenchmarkTests
{
    [TestMethod]
    public void Execute_AtQualificationFanout_ShouldPreserveResultsAndStableCounterOracle()
    {
        foreach (var scenario in new[]
                 {
                     StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableCheapFilter,
                     StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableExpensiveFilter
                 })
        {
            var benchmark = new StabilityAwareScalarReuseQualificationBenchmark
            {
                Fanout = 8,
                Scenario = scenario
            };

            benchmark.Setup();
            try
            {
                var off = benchmark.ExecuteOff();
                var offReads = StabilityAwareScalarReuseQualificationBenchmark.ReuseCounters.GetterReads;
                var on = benchmark.ExecuteOn();
                var onReads = StabilityAwareScalarReuseQualificationBenchmark.ReuseCounters.GetterReads;

                Assert.AreEqual(off, on, scenario.ToString());
                Assert.AreEqual(128, offReads, scenario.ToString());
                Assert.AreEqual(64, onReads, scenario.ToString());
            }
            finally
            {
                benchmark.Cleanup();
            }
        }
    }

    [TestMethod]
    public void Execute_VolatileFilter_ShouldKeepPerUseEvaluation()
    {
        var benchmark = new StabilityAwareScalarReuseQualificationBenchmark
        {
            Fanout = 8,
            Scenario = StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.VolatileFilter
        };

        benchmark.Setup();
        try
        {
            var off = benchmark.ExecuteOff();
            var offReads = StabilityAwareScalarReuseQualificationBenchmark.ReuseCounters.GetterReads;
            var on = benchmark.ExecuteOn();
            var onReads = StabilityAwareScalarReuseQualificationBenchmark.ReuseCounters.GetterReads;

            Assert.AreEqual(off, on);
            Assert.AreEqual(128, offReads);
            Assert.AreEqual(128, onReads);
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    [TestMethod]
    public void Execute_StableAggregate_ShouldPreserveResultAcrossToggle()
    {
        var benchmark = new StabilityAwareScalarReuseQualificationBenchmark
        {
            Fanout = 8,
            Scenario = StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableAggregate
        };

        benchmark.Setup();
        try
        {
            Assert.AreEqual(benchmark.ExecuteOff(), benchmark.ExecuteOn());
        }
        finally
        {
            benchmark.Cleanup();
        }
    }
}
