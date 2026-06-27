using Musoq.Schema;

namespace Musoq.Benchmarks;

public class OptBenchSchemaProvider(List<OptBenchEntity> data) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new OptBenchSchema(data);
    }
}
