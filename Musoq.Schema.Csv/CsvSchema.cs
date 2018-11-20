using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Schema.Csv
{
    public class CsvSchema : SchemaBase
    {
        private const string FileTable = "file";
        private const string SchemaName = "csv";

        public CsvSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            if (name.ToLowerInvariant() == FileTable)
            {
                parameters[3] = Convert.ToInt32(parameters[3]);
                return (CsvBasedTable)Activator.CreateInstance(typeof(CsvBasedTable), parameters);
            }

            throw new NotSupportedException($"Unrecognized table {name}.");
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FileTable:
                    parameters[3] = Convert.ToInt32(parameters[3]);
                    return (CsvSource) Activator.CreateInstance(typeof(CsvSource), parameters.ExpandParameters(interCommunicator));
            }

            throw new NotSupportedException($"Unrecognized method {name}.");
        }

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<CsvSource>(FileTable));

            return constructors.ToArray();
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new CsvLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}