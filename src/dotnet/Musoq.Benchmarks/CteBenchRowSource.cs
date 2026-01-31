using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class CteBenchRowSource : RowSource
{
    private static int _cteCounter;
    private readonly List<CteBenchEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public CteBenchRowSource(List<CteBenchEntity> entities, int simulatedWorkIterations = 0)
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            // Simulate expensive data loading work (like I/O or complex computation)
            // This happens at the START of each CTE - the parallelization should make these overlap
            if (_simulatedWorkIterations > 0)
            {
                var cteId = Interlocked.Increment(ref _cteCounter);
                var startTime = DateTime.UtcNow;
                SimulateWork(_simulatedWorkIterations);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                // Benchmark mode: no console output
            }

            foreach (var entity in _entities) yield return new CteBenchEntityResolver(entity);
        }
    }

    private static void SimulateWork(int iterations)
    {
        var result = 0.0;
        for (var i = 0; i < iterations; i++) result += Math.Sin(i) * Math.Cos(i);

        GC.KeepAlive(result);
    }
}
