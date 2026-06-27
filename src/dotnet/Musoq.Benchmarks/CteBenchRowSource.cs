using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class CteBenchRowSource(List<CteBenchEntity> entities, int simulatedWorkIterations = 0)
    : RowSource<CteBenchEntity>
{
    public override IEnumerable<IReadOnlyList<CteBenchEntity>> Chunks
    {
        get
        {
            // Simulate expensive data loading work (like I/O or complex computation)
            // This happens at the START of each CTE - the parallelization should make these overlap
            if (simulatedWorkIterations > 0)
            {
                SimulateWork(simulatedWorkIterations);
                // Benchmark mode: no console output
            }

            yield return entities;
        }
    }

    private static void SimulateWork(int iterations)
    {
        var result = 0.0;
        for (var i = 0; i < iterations; i++) result += Math.Sin(i) * Math.Cos(i);

        GC.KeepAlive(result);
    }
}
