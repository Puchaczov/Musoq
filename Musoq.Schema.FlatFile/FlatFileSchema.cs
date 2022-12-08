using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using System.Collections.Generic;

namespace Musoq.Schema.FlatFile
{
    /// <summary>
    /// This library allows for reading flat files.
    /// </summary>
    public class FlatFileSchema : SchemaBase
    {
        private const string SchemaName = "Flat";

        /// <virtual-constructors>
        /// <virtual-constructor>
        /// <virtual-param>Path of the given file</virtual-param>
        /// <examples>
        /// <example>
        /// <from>from #flat.file('C:\\Users\\user\\Desktop\\file.log')</from>
        /// <columns>
        /// <column name="LineNumber" type="int">Line number of a given file</column>
        /// <column name="Line" type="string">Line of a given file</column>
        /// </columns>
        /// </example>
        /// </examples>
        /// </virtual-constructor>
        /// </virtual-constructors>
        public FlatFileSchema()
            : base(SchemaName, CreateLibrary())
        {
        }
        
        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "file":
                    return new FlatFileTable();
            }

            throw new TableNotFoundException(nameof(name));
        }

        public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "file":
                    return new FlatFileSource((string)parameters[0], interCommunicator);
            }

            throw new SourceNotFoundException(nameof(name));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new FlatFileLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }

        public override SchemaMethodInfo[] GetConstructors()
        {
            var constructors = new List<SchemaMethodInfo>();

            constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<FlatFileSource>("file"));

            return constructors.ToArray();
        }
    }
}