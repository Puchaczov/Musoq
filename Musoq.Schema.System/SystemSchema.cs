using System;
using Musoq.Plugins;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.System
{
    public class SystemSchema : SchemaBase
    {
        private const string Dual = "dual";
        private const string System = "system";
        public SystemSchema() 
            : base(System, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case Dual:
                    return new DualTable();
            }

            throw new NotSupportedException(name);
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case Dual:
                    return new DualRowSource();
            }

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new EmptyLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }

        private class EmptyLibrary : LibraryBase
        { }
    }
}
