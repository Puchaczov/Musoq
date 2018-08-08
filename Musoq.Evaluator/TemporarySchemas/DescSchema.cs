using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.TemporarySchemas
{
    public class DescSchema : SchemaBase
    {
        private readonly ISchemaColumn[] _columns;
        private readonly ISchemaTable _table;

        public DescSchema(string name, ISchemaTable table, ISchemaColumn[] columns)
            : base(name, null)
        {
            _table = table;
            _columns = columns;
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            return _table;
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            return new TableMetadataSource(_columns);
        }
    }
}