using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class HashOptionalSchemaComprehensiveTests
{
    [TestMethod]
    public void HashOptional_DescSchemaMethodWithParentheses_ShouldWork()
    {
        var query = "desc A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one column");
        Assert.IsTrue(table.Any(row => (string)row[0] == "Name"), "Should contain 'Name' column");
    }

    [TestMethod]
    public void HashOptional_DescFunctionsSchema_ShouldWork()
    {
        var query = "desc functions A";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
    }

    [TestMethod]
    public void HashOptional_DescFunctionsSchemaMethod_ShouldWork()
    {
        var query = "desc functions A.entities";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one library method");
    }

    [TestMethod]
    public void HashOptional_DescFunctionsSchemaMethodWithParentheses_ShouldWork()
    {
        var query = "desc functions A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
    }



    [TestMethod]
    public void HashOptional_CoupleStatement_ShouldWork()
    {
        const string query = "table DummyTable {" +
                             "   Name: string" +
                             "};" +
                             "couple A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Name from SourceOfDummyRows();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("First"),
                    new BasicEntity("Second"),
                    new BasicEntity("Third")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(3, table.Count, "Result should contain exactly 3 strings");
        Assert.IsTrue(table.Any(row => (string)row[0] == "First"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Second"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Third"));
    }

    [TestMethod]
    public void HashOptional_CoupleStatementWithMultipleColumns_ShouldWork()
    {
        const string query = "table DataTable {" +
                             "   Country: string," +
                             "   Population: decimal" +
                             "};" +
                             "couple A.Entities with table DataTable as SourceOfData;" +
                             "select Country, Population from SourceOfData();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland", Population = 38 },
                    new BasicEntity { Country = "Germany", Population = 83 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(2, table.Count, "Result should contain exactly 2 rows");
    }

    [TestMethod]
    public void HashOptional_CoupleStatementMixedWithHashSyntax_ShouldWork()
    {
        const string query = "table DummyTable {" +
                             "   Name: string" +
                             "};" +
                             "couple A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Name from SourceOfDummyRows();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

}
