using System.Linq;

namespace Musoq.Schema.System
{
    internal class RangeTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => RangeHelper.RangeColumns;

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}