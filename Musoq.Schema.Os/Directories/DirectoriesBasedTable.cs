using System.Linq;

namespace Musoq.Schema.Os.Directories
{
    public class DirectoriesBasedTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaDirectoriesHelper.DirectoriesColumns;

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}