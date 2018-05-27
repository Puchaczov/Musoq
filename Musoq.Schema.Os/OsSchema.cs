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

        public override ISchemaTable GetTableByName(string name, string[] parameters)
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

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    return new FilesSource(parameters[0], TryRecognizeBoolean(parameters[1]));
                case DirectoriesTable:
                    return new DirectoriesSource(parameters[0], TryRecognizeBoolean(parameters[1]));
                case ZipTable:
                    return new ZipSource(parameters[0]);
                case ProcessesName:
                    return new ProcessesSource();
                case Self:
                    return new OsSource();
            }

            throw new NotSupportedException();
        }

        private bool TryRecognizeBoolean(string str)
        {
            str = str.Trim().ToLowerInvariant();
            if (str == "1")
                return true;
            if (str == "0")
                return false;
            if (str == "true")
                return true;
            if (str == "false")
                return false;

            throw new NotSupportedException($"value('{str}') as {nameof(Boolean)}");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new DiskLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}