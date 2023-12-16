using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

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
}