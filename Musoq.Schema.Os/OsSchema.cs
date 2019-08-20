using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Os.Compare.Directories;
using Musoq.Schema.Os.Directories;
using Musoq.Schema.Os.Dlls;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Process;
using Musoq.Schema.Os.Self;
using Musoq.Schema.Os.Zip;
using Musoq.Schema.Reflection;

namespace Musoq.Schema.Os
{
    public class OsSchema : SchemaBase
    {
        private const string DirectoriesTable = "directories";
        private const string FilesTable = "files";
        private const string DllsTable = "dlls";
        private const string ZipTable = "zip";
        private const string Self = "self";
        private const string SchemaName = "os";
        private const string ProcessesName = "process";
        private const string DirsCompare = "dirscompare";

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
                case DllsTable:
                    return new DllBasedTable();
                case DirsCompare:
                    return new DirsCompareBasedTable();
            }

            throw new NotSupportedException($"Unsupported table {name}.");
        }

        public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
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
                case DllsTable:
                    return new DllSource((string)parameters[0], (bool)parameters[1], interCommunicator);
                case DirsCompare:
                    return new CompareDirectoriesSource((string)parameters[0], (string)parameters[1], interCommunicator);
            }

            throw new NotSupportedException($"Unsupported row source {name}");
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

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<FilesSource>(FilesTable));
            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<DirectoriesSource>(DirectoriesTable));
            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<ZipSource>(ZipTable));
            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<ProcessesSource>(ProcessesName));
            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<OsSource>(Self));
            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<DllSource>(DllsTable));

            return constructors.ToArray();
        }
    }
}