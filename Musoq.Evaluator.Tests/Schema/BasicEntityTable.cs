using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema
{
    public class BasicEntityTable : ISchemaTable
    {
        public BasicEntityTable()
        {
            Columns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(BasicEntity.Name), 10,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Name)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.City), 11,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.City)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Country), 12,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Country)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Population), 13,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Population)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Self), 14,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Self)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Money), 15,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Money)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Month), 16,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Month)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Time), 17,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Time)).PropertyType),
                new SchemaColumn(nameof(BasicEntity.Id), 18,
                    typeof(BasicEntity).GetProperty(nameof(BasicEntity.Id)).PropertyType)
            };
        }

        public ISchemaColumn[] Columns { get; }
    }
}