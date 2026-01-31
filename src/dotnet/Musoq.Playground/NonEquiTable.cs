using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Playground;

public class NonEquiTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn(nameof(NonEquiEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(NonEquiEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(NonEquiEntity.Population), 2, typeof(int))
    };

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.Single(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(NonEquiEntity));
}
