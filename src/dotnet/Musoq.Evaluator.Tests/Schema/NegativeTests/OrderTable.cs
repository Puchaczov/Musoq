using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class OrderTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(OrderEntity.OrderId), 0, typeof(int)),
        new SchemaColumn(nameof(OrderEntity.PersonId), 1, typeof(int)),
        new SchemaColumn(nameof(OrderEntity.Amount), 2, typeof(decimal)),
        new SchemaColumn(nameof(OrderEntity.Status), 3, typeof(string)),
        new SchemaColumn(nameof(OrderEntity.OrderDate), 4, typeof(System.DateTime)),
        new SchemaColumn(nameof(OrderEntity.Notes), 5, typeof(string))
    ];

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(OrderEntity));
}
