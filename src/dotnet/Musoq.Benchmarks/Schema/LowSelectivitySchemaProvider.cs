using Musoq.Schema;

namespace Musoq.Benchmarks.Schema;

public class LowSelectivitySchemaProvider(IEnumerable<NonEquiEntity> entitiesA, IEnumerable<NonEquiEntity> entitiesB)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new LowSelectivitySchema(entitiesA, entitiesB);
    }
}
