using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Schema.Time
{
    public class TimeSchema : SchemaBase
    {
        public TimeSchema() : base("time", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "interval":
                    return new TimeTable();
            }

            throw new NotSupportedException($"Table {name} not found.");
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "interval":
                    return new TimeSource(
                        DateTimeOffset.Parse((string)parameters[0]), 
                        DateTimeOffset.Parse((string)parameters[1]),
                        (string)parameters[2], 
                        interCommunicator);
            }

            throw new NotSupportedException($"Table {name} not found.");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new TimeLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<TimeSource>("interval"));

            return constructors.ToArray();
        }
    }
}