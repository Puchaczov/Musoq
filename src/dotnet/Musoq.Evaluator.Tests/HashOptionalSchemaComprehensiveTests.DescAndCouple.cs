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

}
