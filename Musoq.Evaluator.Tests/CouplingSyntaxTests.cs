using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CouplingSyntaxTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenSingleValueTableIsCoupledWithSchema_ShouldHaveAppropriateTypesTest()
    {
        const string query = "table DummyTable {" +
                             "   Name 'System.String'" +
                             "};" +
                             "couple #A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Name from SourceOfDummyRows();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                new[]
                {
                    new BasicEntity("ABCAACBA"),
                    new BasicEntity("AAeqwgQEW"),
                    new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                }
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("ABCAACBA", table[0].Values[0]);
        Assert.AreEqual("AAeqwgQEW", table[1].Values[0]);
        Assert.AreEqual("XXX", table[2].Values[0]);
        Assert.AreEqual("dadsqqAA", table[3].Values[0]);
    }
    
    [TestMethod]
    public void WhenTwoValuesTableIsCoupledWithSchema_ShouldHaveAppropriateTypesTest()
    {
        const string query = "table DummyTable {" +
                             "   Country 'System.String'," +
                             "   Population 'System.Decimal'" +
                             "};" +
                             "couple #A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Country, Population from SourceOfDummyRows();";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                new[]
                {
                    new BasicEntity("ABCAACBA", 10),
                    new BasicEntity("AAeqwgQEW", 20),
                    new BasicEntity("XXX", 30),
                    new BasicEntity("dadsqqAA", 40)
                }
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("Population", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("ABCAACBA", table[0].Values[0]);
        Assert.AreEqual(10m, table[0].Values[1]);
        
        Assert.AreEqual("AAeqwgQEW", table[1].Values[0]);
        Assert.AreEqual(20m, table[1].Values[1]);
        
        Assert.AreEqual("XXX", table[2].Values[0]);
        Assert.AreEqual(30m, table[2].Values[1]);
        
        Assert.AreEqual("dadsqqAA", table[3].Values[0]);
        Assert.AreEqual(40m, table[3].Values[1]);
    }
    
    [TestMethod]
    public void WhenTwoTablesAreCoupledWithSchemas_ShouldHaveAppropriateTypesTest()
    {
        const string query = "table FirstTable {" +
                             "   Country 'System.String'," +
                             "   Population 'System.Decimal'" +
                             "};" +
                             "table SecondTable {" +
                             "   Name 'System.String'," +
                             "};" +
                             "couple #A.Entities with table FirstTable as SourceOfFirstTableRows;" +
                             "couple #B.Entities with table SecondTable as SourceOfSecondTableRows;" +
                             "select s2.Name from SourceOfFirstTableRows() s1 inner join SourceOfSecondTableRows() s2 on s1.Country = s2.Name";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                new[]
                {
                    new BasicEntity("ABCAACBA", 10),
                    new BasicEntity("AAeqwgQEW", 20),
                    new BasicEntity("XXX", 30),
                    new BasicEntity("dadsqqAA", 40)
                }
            },
            {
                "#B",
                new[]
                {
                    new BasicEntity("ABCAACBA"),
                    new BasicEntity("AAeqwgQEW"),
                    new BasicEntity("XXX"),
                    new BasicEntity("dadsqqAA")
                }
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("s2.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("ABCAACBA", table[0].Values[0]);
        Assert.AreEqual("AAeqwgQEW", table[1].Values[0]);
        Assert.AreEqual("XXX", table[2].Values[0]);
        Assert.AreEqual("dadsqqAA", table[3].Values[0]);
    }
    
    [TestMethod]
    public void WhenArgumentPassedToAliasedSchema_ShouldBeProperlyRecognized()
    {
        const string query = "table DummyTable {" +
                             "   Parameter0 'System.Boolean'," +
                             "   Parameter1 'System.String'" +
                             "};" +
                             "couple #A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Parameter0, Parameter1 from SourceOfDummyRows(true, 'test');";

        var vm = CreateAndRunVirtualMachine(query, null, new ParametersSchemaProvider());
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        
        Assert.AreEqual("Parameter0", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(bool), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual("Parameter1", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(true, table[0].Values[0]);
        Assert.AreEqual("test", table[0].Values[1]);
    }

    [TestMethod]
    public void WhenDataSourceIsBasedOnAnotherDataSource_ShouldPass()
    {
        var query = "" +
                    "table Values {" +
                    "   Text 'System.String'" +
                    "};" +
                    "couple #unknown.others with table Values as SourceOfValues;" +
                    "with Anything as (" +
                    "   select Text from #unknown.anything()" +
                    ")" +
                    "select Text from SourceOfValues(Anything)";

        dynamic first = new ExpandoObject();

        first.Text = "test1";

        dynamic second = new ExpandoObject();

        second.Text = "test2";

        var vm = CreateAndRunVirtualMachine(query, null, new UnknownSchemaProvider(new[] {first, second}));
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Text", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(2, table.Count);

        Assert.AreEqual("test1", table[0].Values[0]);
        Assert.AreEqual("test2", table[1].Values[0]);
    }

    private class ParametersSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new ParametersSchema();
        }
    }
    
    private class ParametersSchema : SchemaBase
    {
        private const string SchemaName = "Parameters";
    
        public ParametersSchema()
            : base(SchemaName, CreateLibrary())
        {
        }
    
        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new ParametersTable(parameters);
        }
    
        public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            return new ParametersRowsSource(parameters);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();

            var lib = new DynamicLibrary();

            methodManager.RegisterLibraries(lib);

            return new MethodsAggregator(methodManager);
        }
    }
    
    private class ParametersTable : ISchemaTable
    {
        public ParametersTable(object[] values)
        {
            Columns = values
                .Select((t, i) => new SchemaColumn($"Parameter{i}", i, t.GetType()))
                .Cast<ISchemaColumn>()
                .ToArray();
        }
        
        public ISchemaColumn[] Columns { get; }

        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
        
        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.First(c => c.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(c => c.ColumnName == name).ToArray();
        }
    }
    
    private class ParametersRowsSource(object[] values) : RowSourceBase<dynamic>
    {
        protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
        {
            var index = 0;
            var indexToNameMap = new Dictionary<int, string>();
            var accessMap = new Dictionary<string, object>();
            
            foreach (var value in values)
            {
                indexToNameMap.Add(index, $"Parameter{index}");
                accessMap.Add($"Parameter{index}", value);
                index++;
            }
            
            chunkedSource.Add(new List<IObjectResolver>
            {
                new DynamicDictionaryResolver(accessMap, indexToNameMap)
            });
        }
    }
}