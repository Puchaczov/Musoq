using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class CseTestEntityResolver : IObjectResolver
{
    private readonly CseTestEntity _entity;

    public CseTestEntityResolver(CseTestEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public bool HasColumn(string name)
    {
        return name switch
        {
            nameof(CseTestEntity.Id) => true,
            nameof(CseTestEntity.Name) => true,
            nameof(CseTestEntity.Value) => true,
            nameof(CseTestEntity.Category) => true,
            _ => false
        };
    }

    public object this[string name] => name switch
    {
        nameof(CseTestEntity.Id) => _entity.Id,
        nameof(CseTestEntity.Name) => _entity.Name,
        nameof(CseTestEntity.Value) => _entity.Value,
        nameof(CseTestEntity.Category) => _entity.Category,
        _ => throw new KeyNotFoundException($"Column {name} not found")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Value,
        3 => _entity.Category,
        _ => throw new IndexOutOfRangeException()
    };
}
