using Musoq.Schema;

namespace Musoq.Benchmarks;

public class CseTestTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn(nameof(CseTestEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(CseTestEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(CseTestEntity.Value), 2, typeof(int)),
        new SchemaColumn(nameof(CseTestEntity.Category), 3, typeof(string))
    };

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata => new(typeof(CseTestEntity));
}
