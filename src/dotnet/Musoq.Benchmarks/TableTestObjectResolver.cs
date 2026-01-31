using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class TableTestObjectResolver : IObjectResolver
{
    private readonly TableTestEntity _entity;

    public TableTestObjectResolver(TableTestEntity entity)
    {
        _entity = entity;
    }

    public object[] Contexts => Array.Empty<object>();

    public bool HasColumn(string name)
    {
        return name switch
        {
            "Id" or "Name" or "Value" or "Category" => true,
            _ => false
        };
    }

    public object this[string name] => name switch
    {
        "Id" => _entity.Id,
        "Name" => _entity.Name,
        "Value" => _entity.Value,
        "Category" => _entity.Category,
        _ => throw new KeyNotFoundException($"Column '{name}' not found")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Value,
        3 => _entity.Category,
        _ => throw new IndexOutOfRangeException($"Index {index} is out of range")
    };
}
