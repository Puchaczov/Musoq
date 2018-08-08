using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Json
{
    public class JsonSchema : SchemaBase
    {
        private const string FileTable = "file";
        private const string SchemaName = "json";

        public JsonSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            return new JsonBasedTable((string)parameters[1], (string)parameters[2]);
        }

        public override RowSource GetRowSource(string name, InterCommunicator communicator, params object[] parameters)
        {
            return new JsonSource((string)parameters[0], communicator);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new JsonLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}