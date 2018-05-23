using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

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

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            if (name.ToLowerInvariant() == FileTable)
                return new CsvBasedTable(parameters[0], parameters[1]);

            throw new NotSupportedException();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FileTable:
                    return new CsvSource(parameters[0], parameters[1]);
            }

            throw new NotSupportedException();
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