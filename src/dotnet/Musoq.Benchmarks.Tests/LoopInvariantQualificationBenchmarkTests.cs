using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LoopInvariantQualificationBenchmarkTests
{
    [TestMethod]
    public void Execute_AtQualificationFanout_ShouldPreserveRowAndCounterOracles()
    {
        foreach (var scenario in Enum.GetValues<LoopInvariantQualificationBenchmark.QualificationScenario>())
        foreach (var enabled in new[] { false, true })
        {
            var benchmark = new LoopInvariantQualificationBenchmark
            {
                Fanout = 8,
                Scenario = scenario,
                LicmEnabled = enabled
            };

            benchmark.Setup();
            try
            {
                Assert.AreNotEqual(0L, benchmark.Execute(), $"{scenario}/{enabled}");
            }
            finally
            {
                benchmark.Cleanup();
            }
        }
    }
}
