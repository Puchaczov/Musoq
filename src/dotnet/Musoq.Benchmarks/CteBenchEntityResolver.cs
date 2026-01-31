using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class CteBenchEntityResolver : IObjectResolver
{
    private readonly CteBenchEntity _entity;

    public CteBenchEntityResolver(CteBenchEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public object this[string name] => name switch
    {
        "Id" => _entity.Id,
        "Name" => _entity.Name,
        "Value" => _entity.Value,
        "Category" => _entity.Category,
        _ => throw new ArgumentException($"Unknown column: {name}")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Value,
        3 => _entity.Category,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public bool HasColumn(string name)
    {
        return name is "Id" or "Name" or "Value" or "Category";
    }
}
