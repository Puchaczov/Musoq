using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Disk.Directories;
using Musoq.Schema.Disk.Files;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Disk
{
    public class DiskSchema : SchemaBase
    {
        private const string DirectoriesTable = "directories";
        private const string FilesTable = "files";
        private const string SchemaName = "disk";

        public DiskSchema()
            : base(SchemaName, CreateLibrary())
        { }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case FilesTable:
                    return new FilesBasedTable();
                case DirectoriesTable:
                    return new DirectoriesBasedTable();
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