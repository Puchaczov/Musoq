using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class UsedColumnsOrUsedWhereTable : ISchemaTable
{
    public UsedColumnsOrUsedWhereTable()
    {
    }

    public UsedColumnsOrUsedWhereTable(string name)
    {
    }

    public ISchemaColumn[] Columns { get; } = {
        new SchemaColumn(nameof(UsedColumnsOrUsedWhereEntity.Name), 10,
            typeof(BasicEntity).GetProperty(nameof(UsedColumnsOrUsedWhereEntity.Name))!.PropertyType),
        new SchemaColumn(nameof(BasicEntity.City), 11,
            typeof(BasicEntity).GetProperty(nameof(UsedColumnsOrUsedWhereEntity.City))!.PropertyType),
        new SchemaColumn(nameof(UsedColumnsOrUsedWhereEntity.Country), 12,
            typeof(BasicEntity).GetProperty(nameof(UsedColumnsOrUsedWhereEntity.Country))!.PropertyType),
        new SchemaColumn(nameof(UsedColumnsOrUsedWhereEntity.Population), 13,
            typeof(BasicEntity).GetProperty(nameof(UsedColumnsOrUsedWhereEntity.Population))!.PropertyType),
        new SchemaColumn(nameof(UsedColumnsOrUsedWhereEntity.Month), 14,
            typeof(BasicEntity).GetProperty(nameof(UsedColumnsOrUsedWhereEntity.Month))!.PropertyType)
    };

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(UsedColumnsOrUsedWhereEntity));
}