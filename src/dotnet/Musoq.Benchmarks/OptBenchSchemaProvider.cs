using Musoq.Schema;

namespace Musoq.Benchmarks;

public class OptBenchSchemaProvider : ISchemaProvider
{
    private readonly List<OptBenchEntity> _data;

    public OptBenchSchemaProvider(List<OptBenchEntity> data)
    {
        _data = data;
    }

    public ISchema GetSchema(string schema)
    {
        return new OptBenchSchema(_data);
    }
}
