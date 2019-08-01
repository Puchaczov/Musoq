using System.Linq;

namespace Musoq.Schema.Os.Files
{
    public class DllBasedTable : ISchemaTable
    {
        public DllBasedTable()
        {
            Columns = DllInfosHelper.DllInfosColumns;
        }

        public ISchemaColumn[] Columns { get; }

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}