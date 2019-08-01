using Musoq.Schema;
using System.Linq;

namespace Musoq.Evaluator.Tables
{
    internal class VariableTable : ISchemaTable
    {
        public VariableTable(ISchemaColumn[] columns)
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