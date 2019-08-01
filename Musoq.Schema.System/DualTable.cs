using Musoq.Schema.DataSources;
using System.Linq;

namespace Musoq.Schema.System
{
    public class DualTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[]
        {
            new SchemaColumn(nameof(DualEntity.Dummy), 0, typeof(string)), 
        };

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}