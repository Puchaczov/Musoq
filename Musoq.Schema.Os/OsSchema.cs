using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Os.Directories;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Process;
using Musoq.Schema.Os.Self;
using Musoq.Schema.Os.Zip;

namespace Musoq.Schema.Os
{
    public class OsSchema : SchemaBase
    {
        private const string DirectoriesTable = "directories";
        private const string FilesTable = "files";
        private const string ZipTable = "zip";
        private const string Self = "self";
        private const string SchemaName = "os";
        private const string ProcessesName = "process";

        public OsSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    return new FilesBasedTable();
                case DirectoriesTable:
                    return new DirectoriesBasedTable();
                case ZipTable:
                    return new ZipBasedTable();
                case ProcessesName:
                    return new ProcessBasedTable();
                case Self:
                    return new OsBasedTable();
            }

            throw new NotSupportedException();
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    return new FilesSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case DirectoriesTable:
                    return new DirectoriesSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case ZipTable:
                    return new ZipSource((string)parameters[0], interCommunicator);
                case ProcessesName:
                    return new ProcessesSource(interCommunicator);
                case Self:
                    return new OsSource();
            }

            throw new NotSupportedException();
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new OsLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}