using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive evaluator tests for hash-optional schema syntax (from schema.method() without # prefix).
///     These tests cover full query execution to ensure the evaluator correctly handles
///     both hash and hash-optional schema references through the entire query pipeline.
/// </summary>
[TestClass]
public partial class HashOptionalSchemaComprehensiveTests : BasicEntityTestBase
{


    [TestMethod]
    public void HashOptional_Distinct_ShouldWork()
    {
        var query = "select distinct Name from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test"),
                    new BasicEntity("Test"),
                    new BasicEntity("Other"),
                    new BasicEntity("Test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Test"], ["Other"]);
    }



    [TestMethod]
    public void HashOptional_ArithmeticOperations_ShouldWork()
    {
        var query = "select Population + 10, Population - 5, Population * 2, Population / 2 from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Population + 10", typeof(decimal)), ("Population - 5", typeof(decimal)),
            ("Population * 2", typeof(decimal)), ("Population / 2", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [110m, 95m, 200m, 50m]);
    }



    [TestMethod]
    public void HashOptional_SelectMultipleColumns_ShouldWork()
    {
        var query = "select Name, City, Population from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test1", City = "Warsaw", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)), ("City", typeof(string)), ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Test1", "Warsaw", 100m]);
    }

    [TestMethod]
    public void HashOptional_SelectWithExpression_ShouldWork()
    {
        var query = "select Population * 2 as DoubledPopulation from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "City", Population = 50 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("DoubledPopulation", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [100m]);
    }



    [TestMethod]
    public void HashOptional_WhereEquals_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name = 'Match'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match"), new BasicEntity("NoMatch"), new BasicEntity("Match")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Match"], ["Match"]);
    }

    [TestMethod]
    public void HashOptional_WhereGreaterThan_ShouldWork()
    {
        var query = "select Name, Population from A.Entities() where Population > 50";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Low", Population = 30 },
                    new BasicEntity { Name = "High", Population = 100 },
                    new BasicEntity { Name = "Medium", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["High", 100m]);
    }

    [TestMethod]
    public void HashOptional_WhereWithAndOr_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name = 'A' or Name = 'B'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A"], ["B"]);
    }

    [TestMethod]
    public void HashOptional_WhereWithLike_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name like '%est%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test"), new BasicEntity("Testing"), new BasicEntity("NoMatch")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Test"], ["Testing"]);
    }

    [TestMethod]
    public void HashOptional_WhereWithIn_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name in ('A', 'B', 'C')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("D"), new BasicEntity("E")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A"], ["B"]);
    }

    [TestMethod]
    public void HashOptional_WhereWithIsNull_ShouldWork()
    {
        var query = "select Name from A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("NullValue") { NullableValue = null },
                    new BasicEntity("HasValue") { NullableValue = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["NullValue"]);
    }



    [TestMethod]
    public void HashOptional_GroupByWithCount_ShouldWork()
    {
        var query = "select City, Count(City) from A.Entities() group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Berlin" },
                    new BasicEntity { City = "Berlin" },
                    new BasicEntity { City = "Berlin" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Count(City)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Warsaw", 2L], ["Berlin", 3L]);
    }

    [TestMethod]
    public void HashOptional_GroupByWithSum_ShouldWork()
    {
        var query = "select Country, Sum(Population) from A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland", Population = 100 },
                    new BasicEntity { Country = "Poland", Population = 200 },
                    new BasicEntity { Country = "Germany", Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland", 300m], ["Germany", 150m]);
    }

    [TestMethod]
    public void HashOptional_GroupByWithHaving_ShouldWork()
    {
        var query = "select Country, Count(Country) from A.Entities() group by Country having Count(Country) > 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("Count(Country)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Poland", 2L]);
    }



    [TestMethod]
    public void HashOptional_OrderByAscending_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("C"), new BasicEntity("A"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A"], ["B"], ["C"]);
    }

    [TestMethod]
    public void HashOptional_OrderByDescending_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("C"), new BasicEntity("A"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["C"], ["B"], ["A"]);
    }

}
