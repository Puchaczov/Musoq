using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenNotInSubquery_WithOrCondition_ShouldReturnMatchingRows()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b) OR a.Country = 'GERMANY'"
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
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["BERLIN"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithOrBetweenTwoInSubqueries_ShouldReturnUnion()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (SELECT b.City FROM #B.entities() b)\n"
            + "               OR a.Country IN (SELECT c.Country FROM #C.entities() c)"
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
            { "#B", [new BasicEntity("PARIS", "FRANCE", 300)] },
            { "#C", [new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS"], ["BERLIN"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithAndContainingOrWithSubquery_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (SELECT b.City FROM #B.entities() b)\n"
            + "              AND (a.Population > 400 OR a.Country IN (SELECT c.Country FROM #C.entities() c))"
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
            { "#C", [new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"]);
    }

    [TestMethod]
    public void WhenInSubquery_OrWithNoMatchingRows_ShouldReturnOnlyOrBranch()
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
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            { "#B", [new BasicEntity("TOKYO", "JAPAN", 1000)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }

    // ── Set operator IN subquery tests ────────────────────────────────────────

    [TestMethod]
    public void WhenNotInSubquery_WithUnion_ShouldExcludeUnionResults()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b UNION (City) SELECT c.City FROM #C.entities() c)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 200),
                ]
            },
            { "#B", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#C", [new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PRAGUE"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithChainedUnions_ShouldCombineAll()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                UNION (City) SELECT c.City FROM #C.entities() c\n"
            + "                UNION (City) SELECT d.City FROM #D.entities() d)"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                    new BasicEntity("LONDON", "UK", 400),
                ]
            },
            { "#B", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#C", [new BasicEntity("BERLIN", "GERMANY", 250)] },
            { "#D", [new BasicEntity("PARIS", "FRANCE", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithUnionAndOr_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (SELECT b.City FROM #B.entities() b UNION (City) SELECT c.City FROM #C.entities() c)\n"
            + "               OR a.Country = 'FRANCE'"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                    new BasicEntity("LONDON", "UK", 400),
                ]
            },
            { "#B", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#C", [new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenInSubquery_WithExceptAndMultipleColumns_ShouldFilterCorrectly()
    {
        var query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                EXCEPT (City) SELECT c.City FROM #C.entities() c)\n"
            + "              AND a.Population > 100"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 50),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 200),
                ]
            },
            { "#C", [new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);
    }
}
