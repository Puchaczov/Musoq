using FQL.Schema.DataSources;
using FQL.Schema.Managers;

namespace FQL.Schema.Disk.Console
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
