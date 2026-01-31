using Musoq.Schema;

namespace Musoq.Benchmarks;

public class OptBenchTable : ISchemaTable
{
    public ISchemaColumn[] Columns => new ISchemaColumn[]
    {
        new SchemaColumn(nameof(OptBenchEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(OptBenchEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(OptBenchEntity.Value), 2, typeof(int)),
        new SchemaColumn(nameof(OptBenchEntity.Category), 3, typeof(string))
    };

    public SchemaTableMetadata Metadata => new(typeof(OptBenchEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(c => c.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(c => c.ColumnName == name).ToArray();
    }
}
