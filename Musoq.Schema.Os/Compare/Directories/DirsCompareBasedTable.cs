using System.Linq;

namespace Musoq.Schema.Os.Compare.Directories
{
    public class DirsCompareBasedTable : ISchemaTable
    {

        public ISchemaColumn[] Columns => CompareDirectoriesHelper.CompareDirectoriesColumns;

        public ISchemaColumn GetColumnByName(string name)
        {
            return CompareDirectoriesHelper.CompareDirectoriesColumns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}
