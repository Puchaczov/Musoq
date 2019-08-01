using System.Linq;

namespace Musoq.Schema.Os.Self
{
    public class OsBasedTable : ISchemaTable
    {
        public OsBasedTable()
        {
            Columns = OsHelper.ProcessColumns;
        }

        public ISchemaColumn[] Columns { get; }

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}