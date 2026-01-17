using Musoq.Schema;

namespace Musoq.Benchmarks.Schema.Country;

public class CountryEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(CountryEntity.City), 11,
            typeof(CountryEntity).GetProperty(nameof(CountryEntity.City))!.PropertyType),
        new SchemaColumn(nameof(CountryEntity.Country), 12,
            typeof(CountryEntity).GetProperty(nameof(CountryEntity.Country))!.PropertyType),
        new SchemaColumn(nameof(CountryEntity.Population), 13,
            typeof(CountryEntity).GetProperty(nameof(CountryEntity.Population))!.PropertyType)
    ];

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(CountryEntity));
}