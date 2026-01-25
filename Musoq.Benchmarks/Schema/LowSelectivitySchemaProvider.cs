using Musoq.Schema;

namespace Musoq.Benchmarks.Schema;

public class LowSelectivitySchemaProvider : ISchemaProvider
{
    private readonly IEnumerable<NonEquiEntity> _entitiesA;
    private readonly IEnumerable<NonEquiEntity> _entitiesB;

    public LowSelectivitySchemaProvider(IEnumerable<NonEquiEntity> entitiesA, IEnumerable<NonEquiEntity> entitiesB)
    {
        _entitiesA = entitiesA;
        _entitiesB = entitiesB;
    }

    public ISchema GetSchema(string schema)
    {
        return new LowSelectivitySchema(_entitiesA, _entitiesB);
    }
}
