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

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }
}