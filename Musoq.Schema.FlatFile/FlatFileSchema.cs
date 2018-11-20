using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using System.Collections.Generic;

namespace Musoq.Schema.FlatFile
{
    public class FlatFileSchema : SchemaBase
    {
        private const string SchemaName = "FlatFile";

        public FlatFileSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "file":
                    return new FlatFileTable();
            }

            throw new TableNotFoundException(nameof(name));
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "file":
                    return new FlatFileSource((string)parameters[0], interCommunicator);
            }

            throw new SourceNotFoundException(nameof(name));
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

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<FlatFileSource>("file"));

            return constructors.ToArray();
        }
    }
}