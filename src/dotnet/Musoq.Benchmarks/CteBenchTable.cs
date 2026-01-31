using Musoq.Schema;

namespace Musoq.Benchmarks;

public class CteBenchTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn("Id", 0, typeof(int)),
        new SchemaColumn("Name", 1, typeof(string)),
        new SchemaColumn("Value", 2, typeof(int)),
        new SchemaColumn("Category", 3, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(CteBenchEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }
}
