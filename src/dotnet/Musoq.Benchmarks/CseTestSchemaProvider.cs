using Musoq.Schema;

namespace Musoq.Benchmarks;

public class CseTestSchemaProvider : ISchemaProvider
{
    private readonly IReadOnlyCollection<CseTestEntity> _data;

    public CseTestSchemaProvider(IReadOnlyCollection<CseTestEntity> data)
    {
        _data = data;
    }

    public ISchema GetSchema(string schema)
    {
        return new CseTestSchema(_data);
    }
}
