using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TableTestTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new TableTestColumn("Id", 0, typeof(int)),
        new TableTestColumn("Name", 1, typeof(string)),
        new TableTestColumn("Value", 2, typeof(int)),
        new TableTestColumn("Category", 3, typeof(string))
    };

    public SchemaTableMetadata Metadata => new(typeof(TableTestEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }
}
