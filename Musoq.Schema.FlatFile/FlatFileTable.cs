using System.Linq;

namespace Musoq.Schema.FlatFile
{
    public class FlatFileTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = FlatFileHelper.FlatColumns;

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}