using Musoq.Schema;

namespace Musoq.Evaluator.TemporarySchemas
{
    public class DynamicTable : ISchemaTable
    {
        public DynamicTable(ISchemaColumn[] columns)
        {
            Columns = columns;
        }

        public ISchemaColumn[] Columns { get; }
    }
}