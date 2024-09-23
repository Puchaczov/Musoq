using Musoq.Schema;
using System.Linq;

namespace Musoq.Evaluator.Tables
{
    internal class VariableTable(ISchemaColumn[] columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = columns;

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