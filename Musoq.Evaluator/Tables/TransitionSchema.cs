using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tables
{
    internal class TransitionSchema : SchemaBase
    {
        private readonly ISchemaTable _table;

        public TransitionSchema(string name, ISchemaTable table) 
            : base(name, CreateLibrary())
        {
            _table = table;
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return _table;
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new TransientVariableSource(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new TransitionLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}