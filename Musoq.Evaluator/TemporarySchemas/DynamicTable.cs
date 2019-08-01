using Musoq.Schema;
using System.Linq;

namespace Musoq.Evaluator.TemporarySchemas
{
    public class DynamicTable : ISchemaTable
    {
        public DynamicTable(ISchemaColumn[] columns)
        {
            Columns = columns;
        }

        public ISchemaColumn[] Columns { get; }

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }
    }
}