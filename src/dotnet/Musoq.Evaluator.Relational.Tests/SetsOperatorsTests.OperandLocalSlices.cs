using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{
    [TestMethod]
    public void ExceptWithSkipDoubleSourceTest()
    {
        const string query = (
            "\n"
            + "with left_slice as (select Name from #A.Entities() skip 1),\n"
            + "right_slice as (select Name from #B.Entities() skip 2)\n"
            + "select Name from left_slice except (Name) select Name from right_slice"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("010")],
            ["#B"] = [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("002")],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["010"]);
    }

    [TestMethod]
    public void ExceptWithSkipTripleSourcesTest()
    {
        const string query = (
            "\n"
            + "with first_slice as (select Name from #A.Entities() skip 1),\n"
            + "second_slice as (select Name from #B.Entities() skip 2),\n"
            + "third_slice as (select Name from #C.Entities() skip 3)\n"
            + "select Name from first_slice\n"
            + "except (Name) select Name from second_slice\n"
            + "except (Name) select Name from third_slice"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("001"), new BasicEntity("002")],
            ["#B"] = [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")],
            ["#C"] = [new BasicEntity("005")],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["002"]);
    }

    [TestMethod]
    public void IntersectWithSkipDoubleSourceTest()
    {
        const string query = (
            "\n"
            + "with left_slice as (select Name from #A.Entities() skip 1),\n"
            + "right_slice as (select Name from #B.Entities() skip 2)\n"
            + "select Name from left_slice intersect (Name) select Name from right_slice"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")],
            ["#B"] = [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["005"]);
    }

    [TestMethod]
    public void IntersectWithSkipTripleSourcesTest()
    {
        const string query = (
            "\n"
            + "with first_slice as (select Name from #A.Entities() skip 1),\n"
            + "second_slice as (select Name from #B.Entities() skip 2)\n"
            + "select Name from first_slice\n"
            + "intersect (Name) select Name from second_slice\n"
            + "intersect (Name) select Name from #C.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")],
            ["#B"] = [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")],
            ["#C"] = [new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("005")],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["005"]);
    }
}
