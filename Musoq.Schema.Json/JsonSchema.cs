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
        { }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return new JsonBasedTable(parameters[1], parameters[2]);
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new JsonSource(parameters[0]);
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