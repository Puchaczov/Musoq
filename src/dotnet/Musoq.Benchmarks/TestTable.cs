using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TestTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn("Id", 0, typeof(int)),
        new SchemaColumn("Name", 1, typeof(string)),
        new SchemaColumn("City", 2, typeof(string)),
        new SchemaColumn("Email", 3, typeof(string)),
        new SchemaColumn("Description", 4, typeof(string))
    };

    public SchemaTableMetadata Metadata => new(typeof(TestEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }
}
