using Musoq.Schema;

namespace Musoq.Playground;

internal sealed class NonEquiSchemaProvider(IEnumerable<NonEquiEntity> entities, int simulatedWorkIterations = 0)
    : ISchemaProvider
{
    private readonly IReadOnlyList<NonEquiEntity> _entities = entities as IReadOnlyList<NonEquiEntity> ?? entities.ToArray();

    public ISchema GetSchema(string schema)
    {
        return new NonEquiSchema(_entities, simulatedWorkIterations);
    }
}
