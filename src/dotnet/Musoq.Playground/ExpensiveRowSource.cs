using Musoq.Schema.DataSources;

namespace Musoq.Playground;

public class ExpensiveRowSource : RowSource
{
    private readonly IEnumerable<NonEquiEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public ExpensiveRowSource(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations)
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            var enumId = ExpensiveCteCounter.Increment();
            Console.WriteLine($"  [Enum {enumId}] Thread {Thread.CurrentThread.ManagedThreadId} starting enumeration");

            if (_simulatedWorkIterations > 0) SimulateWork(_simulatedWorkIterations);

            foreach (var entity in _entities) yield return new EntityResolver(entity);
        }
    }

    private static void SimulateWork(int iterations)
    {
        var result = 0.0;
        for (var i = 0; i < iterations; i++) result += Math.Sin(i) * Math.Cos(i);
        GC.KeepAlive(result);
    }
}
