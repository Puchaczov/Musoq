using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class PersonTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(PersonEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(PersonEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(PersonEntity.Age), 2, typeof(int)),
        new SchemaColumn(nameof(PersonEntity.City), 3, typeof(string)),
        new SchemaColumn(nameof(PersonEntity.Salary), 4, typeof(decimal)),
        new SchemaColumn(nameof(PersonEntity.BirthDate), 5, typeof(System.DateTime)),
        new SchemaColumn(nameof(PersonEntity.ManagerId), 6, typeof(int?)),
        new SchemaColumn(nameof(PersonEntity.Email), 7, typeof(string))
    ];

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(PersonEntity));
}
