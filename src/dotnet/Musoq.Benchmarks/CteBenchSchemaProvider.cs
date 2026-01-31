using Musoq.Schema;

namespace Musoq.Benchmarks;

public class CteBenchSchemaProvider : ISchemaProvider
{
    private readonly List<CteBenchEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public CteBenchSchemaProvider(List<CteBenchEntity> entities, int simulatedWorkIterations = 0)
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public ISchema GetSchema(string schema)
    {
        return new CteBenchSchema(_entities, _simulatedWorkIterations);
    }
}
