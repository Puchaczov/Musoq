using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class TypesTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(TypesEntity.IntCol), 0, typeof(int)),
        new SchemaColumn(nameof(TypesEntity.LongCol), 1, typeof(long)),
        new SchemaColumn(nameof(TypesEntity.ShortCol), 2, typeof(short)),
        new SchemaColumn(nameof(TypesEntity.ByteCol), 3, typeof(byte)),
        new SchemaColumn(nameof(TypesEntity.DecimalCol), 4, typeof(decimal)),
        new SchemaColumn(nameof(TypesEntity.DoubleCol), 5, typeof(double)),
        new SchemaColumn(nameof(TypesEntity.FloatCol), 6, typeof(float)),
        new SchemaColumn(nameof(TypesEntity.BoolCol), 7, typeof(bool)),
        new SchemaColumn(nameof(TypesEntity.StringCol), 8, typeof(string)),
        new SchemaColumn(nameof(TypesEntity.DateTimeCol), 9, typeof(DateTime)),
        new SchemaColumn(nameof(TypesEntity.DateTimeOffsetCol), 10, typeof(DateTimeOffset)),
        new SchemaColumn(nameof(TypesEntity.GuidCol), 11, typeof(Guid)),
        new SchemaColumn(nameof(TypesEntity.NullableIntCol), 12, typeof(int?))
    ];

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(TypesEntity));
}
