using System;
using FQL.Schema.DataSources;
using FQL.Schema.Managers;

namespace FQL.Schema.Disk.Disk
{
    public class DiskSchema : SchemaBase
    {
        private const string DirectoryTable = "directory";
        private const string SchemaName = "disk";

        public DiskSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case DirectoryTable:
                    return new DirectoryBasedTable();
            }

            throw new NotSupportedException();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case DirectoryTable:
                    return new FilesSource(parameters[0], TryRecognizeBoolean(parameters[1]));
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