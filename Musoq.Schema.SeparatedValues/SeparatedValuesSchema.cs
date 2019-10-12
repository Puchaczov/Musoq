using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.SeparatedValues
{
    public class SeparatedValuesSchema : SchemaBase
    {
        private const string SchemaName = "SeparatedValues";

        public SeparatedValuesSchema()
            : base(SchemaName, CreateLibrary())
        {
            AddSource<SeparatedValuesSource>("csv");
            AddSource<SeparatedValuesSource>("tsv");
            AddTable<SeparatedValuesTable>("csv");
            AddTable<SeparatedValuesTable>("tsv");
        }

        public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "csv":
                    if (parameters[0] is IReadOnlyTable csvTable)
                        return new SeparatedValuesSource(csvTable, ",", interCommunicator);

                    return new SeparatedValuesSource((string)parameters[0], ",", (bool)parameters[1], (int)parameters[2], interCommunicator);
                case "tsv":
                    if (parameters[0] is IReadOnlyTable tsvTable)
                        return new SeparatedValuesSource(tsvTable, "\t", interCommunicator);

                    return new SeparatedValuesSource((string)parameters[0], "\t", (bool)parameters[1], (int)parameters[2], interCommunicator);
            }

            return base.GetRowSource(name, interCommunicator, parameters);
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "csv":
                    return new SeparatedValuesTable((string)parameters[0], ",", (bool)parameters[1], (int)parameters[2]);
                case "tsv":
                    return new SeparatedValuesTable((string)parameters[0], "\t", (bool)parameters[1], (int)parameters[2]);
            }

            return base.GetTableByName(name, parameters);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new SeparatedValuesLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}