using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.FlatFile
{
    public class FlatFileSchema : SchemaBase
    {
        private const string SchemaName = "FlatFile";

        public FlatFileSchema() 
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return new FlatFileTable();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new FlatFileSource(parameters[0]);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new FlatFileLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}