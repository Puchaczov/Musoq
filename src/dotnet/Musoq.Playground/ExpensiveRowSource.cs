using Musoq.Schema.DataSources;

namespace Musoq.Playground;

internal sealed class ExpensiveRowSource(IReadOnlyList<NonEquiEntity> entities, int simulatedWorkIterations)
    : RowSource<NonEquiEntity>
{
    public override IEnumerable<IReadOnlyList<NonEquiEntity>> Chunks
    {
        get
        {
            var enumId = ExpensiveCteCounter.Increment();
            Console.WriteLine($"  [Enum {enumId}] Thread {Environment.CurrentManagedThreadId} starting enumeration");

            if (simulatedWorkIterations > 0) SimulateWork(simulatedWorkIterations);

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
