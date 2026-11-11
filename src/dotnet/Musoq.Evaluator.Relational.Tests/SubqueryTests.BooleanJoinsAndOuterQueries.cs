using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenMultipleInSubqueries_WithAndCondition_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (SELECT b.City FROM #B.entities() b)\n"
            + "              AND a.Country IN (SELECT c.Country FROM #C.entities() c)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            { "#C", [new BasicEntity("KRAKOW", "POLAND", 400)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithOrCondition_ShouldReturnMatchingRows()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (SELECT b.City FROM #B.entities() b) OR a.Population > 400"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 450),
                ]
            },
            { "#B", [new BasicEntity("WARSAW", "POLAND", 500)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithOrBetweenNonSubqueryConditions_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (SELECT b.City FROM #B.entities() b) AND (a.Population > 400 OR a.Country = 'GERMANY')"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            { "#B", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"]);
    }

    [TestMethod]
    public void WhenInSubquery_NestedInSubquery_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.Country IN (\n"
            + "                SELECT b.Country FROM #B.entities() b\n"
            + "                WHERE b.City IN (SELECT c.City FROM #C.entities() c)\n"
            + "            )"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 400), new BasicEntity("MUNICH", "GERMANY", 200)] },
            { "#C", [new BasicEntity("KRAKOW", "POLAND", 400)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithExistingInnerJoin_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City, b.Country FROM #A.entities() a\n"
            + "            INNER JOIN #B.entities() b ON a.Country = b.Country\n"
            + "            WHERE a.City IN (SELECT c.City FROM #C.entities() c)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 400), new BasicEntity("MUNICH", "GERMANY", 200)] },
            { "#C", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("PARIS", "FRANCE", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithExistingLeftJoin_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City, b.City FROM #A.entities() a\n"
            + "            LEFT JOIN #B.entities() b ON a.Country = b.Country\n"
            + "            WHERE a.City IN (SELECT c.City FROM #C.entities() c)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 400)] },
            { "#C", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null }
        );
    }
}
