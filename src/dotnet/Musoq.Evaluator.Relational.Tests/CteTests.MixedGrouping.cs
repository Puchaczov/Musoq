using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant1_ShouldPass()
    {
        var query = (
            "\n"
            + "with first as (\n"
            + "    select 1 as Test from #A.entities()\n"
            + "), second as (\n"
            + "    select 2 as Test from #B.entities() group by 'fake'\n"
            + ")\n"
            + "select c.GetBytes(c.Name) from first a\n"
            + "    inner join #A.entities() c on 1 = 1\n"
            + "    inner join second b on 1 = 1"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c.GetBytes(c.Name)", typeof(byte[])));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [Encoding.UTF8.GetBytes("First")]);
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant2_ShouldPass()
    {
        var query = (
            "\n"
            + "with first as (\n"
            + "    select Name from #A.entities()\n"
            + "), second as (\n"
            + "    select Name from #B.entities() group by Name\n"
            + ")\n"
            + "select\n"
            + "    c.GetBytes(c.Name)\n"
            + "from first a\n"
            + "    inner join #A.entities() c on 1 = 1\n"
            + "    inner join second b on 1 = 1"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c.GetBytes(c.Name)", typeof(byte[])));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [Encoding.UTF8.GetBytes("First")]);
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant3_ShouldPass()
    {
        var query = (
            "\n"
            + "with first as (\n"
            + "    select Name from #A.entities()\n"
            + "), second as (\n"
            + "    select Name from #B.entities() group by Name\n"
            + ")\n"
            + "select\n"
            + "    a.Name,\n"
            + "    b.Name,\n"
            + "    c.GetBytes(c.Name)\n"
            + "from first a\n"
            + "    inner join #A.entities() c on 1 = 1\n"
            + "    inner join second b on 1 = 1"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Name", typeof(string)),
            ("c.GetBytes(c.Name)", typeof(byte[]))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["First", "Second", Encoding.UTF8.GetBytes("First")]);
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant4_ShouldPass()
    {
        var query = (
            "\n"
            + "with first as (\n"
            + "    select a.Name as Name from #A.entities() a group by a.Name having Count(a.Name) > 0\n"
            + "), second as (\n"
            + "    select b.Name() as Name from #B.entities() b inner join first a on 1 = 1\n"
            + "), third as (\n"
            + "    select b.Name as Name from second b group by b.Name having Count(b.Name) > 0\n"
            + "), fourth as (\n"
            + "    select\n"
            + "        c.Name() as Name\n"
            + "    from second b\n"
            + "        inner join #A.entities() c on 1 = 1\n"
            + "        inner join third d on 1 = 1\n"
            + ")\n"
            + "select\n"
            + "    c.Name as Name\n"
            + "from fourth c"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] },
            { "#C", [new BasicEntity("Third")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["First"]);
    }
}
