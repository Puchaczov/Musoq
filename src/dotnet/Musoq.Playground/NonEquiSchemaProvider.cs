using Musoq.Schema;

namespace Musoq.Playground;

public class NonEquiSchemaProvider : ISchemaProvider
{
    private readonly IEnumerable<NonEquiEntity> _entities;
    private readonly int _simulatedWorkIterations;

    public NonEquiSchemaProvider(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations = 0)
    {
        _entities = entities;
        _simulatedWorkIterations = simulatedWorkIterations;
    }

    public ISchema GetSchema(string schema)
    {
        return new NonEquiSchema(_entities, _simulatedWorkIterations);
    }
}
