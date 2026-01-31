using Musoq.Schema.DataSources;

namespace Musoq.Playground;

public class EntityResolver : IObjectResolver
{
    private readonly NonEquiEntity _entity;

    public EntityResolver(NonEquiEntity entity)
    {
        _entity = entity;
        Contexts = [entity];
    }

    public object[] Contexts { get; }

    public object this[string name] => name switch
    {
        "Id" => _entity.Id,
        "Name" => _entity.Name,
        "Population" => _entity.Population,
        _ => throw new ArgumentException($"Unknown column: {name}")
    };

    public object this[int index] => index switch
    {
        0 => _entity.Id,
        1 => _entity.Name,
        2 => _entity.Population,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public bool HasColumn(string name)
    {
        return name is "Id" or "Name" or "Population";
    }
}
