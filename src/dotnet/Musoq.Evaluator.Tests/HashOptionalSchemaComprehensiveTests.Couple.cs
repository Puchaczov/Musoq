using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class HashOptionalSchemaComprehensiveTests
{
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["First"],
            ["Second"],
            ["Third"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Poland", 38m],
            ["Germany", 83m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Test"]);
    }
}
