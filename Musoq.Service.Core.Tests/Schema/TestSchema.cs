using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Service.Core.Tests.Schema
{
    public class TestSchema : SchemaBase
    {
        private const string SchemaName = "Test";

        public TestSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "mem":
                    return new TestTable();
            }

            throw new TableNotFoundException(name);
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "mem":
                    return new TestSource();
            }

            throw new SourceNotFoundException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new TestLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}
