using Musoq.Schema;

namespace Musoq.Evaluator.Tables
{
    internal class VariableTable : ISchemaTable
    {
        public VariableTable(ISchemaColumn[] columns)
        {
            Columns = columns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}