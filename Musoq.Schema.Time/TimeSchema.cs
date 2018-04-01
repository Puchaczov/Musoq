using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Time
{
    public class TimeSchema : SchemaBase
    {
        public TimeSchema() : base("time", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "interval":
                    return new TimeTable();
            }

            throw new NotSupportedException($"Table {name} not found.");
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "interval":
                    return new TimeSource(DateTimeOffset.Parse(parameters[0]), DateTimeOffset.Parse(parameters[1]), parameters[2]);
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
    }
}
