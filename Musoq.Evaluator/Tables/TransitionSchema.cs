using System.Collections.Generic;
using System.Reflection;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

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

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            return _table;
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
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

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<TransientVariableSource>("transient"));

            return constructors.ToArray();
        }
    }
}