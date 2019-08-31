using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Schema.System
{
    public partial class SystemSchema : SchemaBase
    {
        private const string Dual = "dual";
        private const string Range = "range";
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
                case Range:
                    return new RangeTable();
            }

            throw new NotSupportedException(name);
        }

        public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case Dual:
                    return new DualRowSource();
                case Range:
                    {
                        switch(parameters.Length)
                        {
                            case 1:
                                return new RangeSource(0, Convert.ToInt64(parameters[0]));
                            case 2:
                                return new RangeSource(Convert.ToInt64(parameters[0]), Convert.ToInt64(parameters[1]));
                        }
                        break;
                    }
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

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<DualRowSource>(Dual));
            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<RangeSource>(Range));

            return constructors.ToArray();
        }
    }
}
