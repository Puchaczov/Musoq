using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

public class TestObjectResolver : IObjectResolver
{
    private readonly TestEntity _entity;

    public TestObjectResolver(TestEntity entity)
    {
        _entity = entity;
    }

    public object[] Contexts => Array.Empty<object>();

    public bool HasColumn(string name)
    {
        return name switch
        {
            "Id" or "Name" or "City" or "Email" or "Description" => true,
            _ => false
        };
    }

    public object this[string name] => name switch
    {
        "Id" => _entity.Id,
        "Name" => _entity.Name,
        "City" => _entity.City,
        "Email" => _entity.Email,
        "Description" => _entity.Description,
        _ => throw new KeyNotFoundException($"Column '{name}' not found")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.City,
        3 => _entity.Email,
        4 => _entity.Description,
        _ => throw new IndexOutOfRangeException($"Index {index} is out of range")
    };
}
