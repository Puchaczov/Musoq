using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class OptBenchEntityResolver : IObjectResolver
{
    private readonly OptBenchEntity _entity;

    public OptBenchEntityResolver(OptBenchEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public object? this[string name] => name switch
    {
        nameof(OptBenchEntity.Id) => _entity.Id,
        nameof(OptBenchEntity.Name) => _entity.Name,
        nameof(OptBenchEntity.Value) => _entity.Value,
        nameof(OptBenchEntity.Category) => _entity.Category,
        _ => null
    };

    public object? this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Value,
        3 => _entity.Category,
        _ => null
    };

    public bool HasColumn(string name)
    {
        return name is
            nameof(OptBenchEntity.Id) or
            nameof(OptBenchEntity.Name) or
            nameof(OptBenchEntity.Value) or
            nameof(OptBenchEntity.Category);
    }
}
