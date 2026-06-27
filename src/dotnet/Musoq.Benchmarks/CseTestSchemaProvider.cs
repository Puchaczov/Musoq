using Musoq.Schema;

namespace Musoq.Benchmarks;

public class CseTestSchemaProvider(IReadOnlyList<CseTestEntity> data) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new CseTestSchema(data);
    }
}
