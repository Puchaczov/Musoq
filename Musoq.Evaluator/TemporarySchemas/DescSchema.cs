using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.TemporarySchemas
{
    public class DescSchema : SchemaBase
    {
        private readonly ISchemaTable _table;
        private readonly ISchemaColumn[] _columns;

        public DescSchema(string name, ISchemaTable table, ISchemaColumn[] columns) 
            : base(name, null)
        {
            _table = table;
            _columns = columns;
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return _table;
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new TableMetadataSource(_columns);
        }
    }
}