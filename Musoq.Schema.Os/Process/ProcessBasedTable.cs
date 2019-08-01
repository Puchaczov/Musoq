using System.Linq;

namespace Musoq.Schema.Os.Process
{
    public class ProcessBasedTable : ISchemaTable
    {
        public ProcessBasedTable()
        {
            Columns = ProcessHelper.ProcessColumns;
        }

        public ISchemaColumn[] Columns { get; }

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}