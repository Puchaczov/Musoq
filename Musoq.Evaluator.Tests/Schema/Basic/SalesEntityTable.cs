using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class SalesEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(SalesEntity.Category), 0,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Category))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Product), 1,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Product))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Month), 2,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Month))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Quarter), 3,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Quarter))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Year), 4,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Year))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Quantity), 5,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Quantity))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Revenue), 6,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Revenue))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.SalesDate), 7,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.SalesDate))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Region), 8,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Region))!.PropertyType),
        new SchemaColumn(nameof(SalesEntity.Salesperson), 9,
            typeof(SalesEntity).GetProperty(nameof(SalesEntity.Salesperson))!.PropertyType)
    ];

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(SalesEntity));
}