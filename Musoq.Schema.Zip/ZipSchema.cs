using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Zip
{
    public class ZipSchema : SchemaBase
    {
        public ZipSchema(string name) 
            : base(name, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return new ZipBasedTable();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new ZipSource(parameters[0]);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new ZipLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}