using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Disk.Console
{
    public class SchemaConsole : SchemaBase
    {
        public SchemaConsole(string name, MethodsAggregator methodsAggregator) 
            : base(name, methodsAggregator)
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return new TextBasedTable();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new ConsoleSource(parameters[0]); ;
        }
    }
}
