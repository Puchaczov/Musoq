using System.Linq;

namespace Musoq.Schema.Os.Files
{
    public class FilesBasedTable : ISchemaTable
    {
        public FilesBasedTable()
        {
            Columns = SchemaFilesHelper.FilesColumns;
        }

        public ISchemaColumn[] Columns { get; }

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}