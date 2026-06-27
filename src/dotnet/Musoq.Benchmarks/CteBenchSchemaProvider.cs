using Musoq.Schema;

namespace Musoq.Benchmarks;

public class CteBenchSchemaProvider(List<CteBenchEntity> entities, int simulatedWorkIterations = 0)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new CteBenchSchema(entities, simulatedWorkIterations);
    }
}
