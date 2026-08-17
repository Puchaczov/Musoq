using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenInSubquery_WithUnionSubquery_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b UNION (City) SELECT c.City FROM #C.entities() c)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 200)
                ]
            },
            {
                "#B", [new BasicEntity("WARSAW", "POLAND", 500)]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"]);
    }

    [TestMethod]
    public void WhenInSubquery_SubqueryHasDuplicates_ShouldNotMultiplyRows()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 400),
                    new BasicEntity("WARSAW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"]);
    }

    [TestMethod]
    public void WhenInSubquery_EmptyOuterSource_ShouldReturnEmpty()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", Array.Empty<BasicEntity>()
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenNotInSubquery_EmptyOuterSource_ShouldReturnEmpty()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", Array.Empty<BasicEntity>()
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenInSubquery_SingleRowMatch_ShouldReturnOne()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"]);
    }

    [TestMethod]
    public void WhenInSubquery_SelectMultipleColumns_ShouldWork()
    {
        var query = @"
            SELECT a.City, a.Country, a.Population FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("a.Country", typeof(string)),
            ("a.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "POLAND", 500m],
            ["PARIS", "FRANCE", 300m]);
    }

    [TestMethod]
    public void WhenInSubquery_WithExpressionOnLeftSide_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE (a.Population + 100) IN (SELECT b.Population FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("X", "Y", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"]);
    }

}
