using System.Linq;

namespace Musoq.Schema.Os.Zip
{
    public class ZipBasedTable : ISchemaTable
    {
        public ZipBasedTable()
        {
            Columns = SchemaZipHelper.SchemaColumns;
        }

        public ISchemaColumn[] Columns { get; }

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}