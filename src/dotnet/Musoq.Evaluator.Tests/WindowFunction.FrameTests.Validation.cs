using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    [TestMethod]
    public void WhenFrameWithAvgRunning_ShouldComputeCorrectAverage()
    {
        var query = @"
            select Name, Avg(Population) over (order by Name rows between unbounded preceding and current row) as RunAvg
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100m },
            new BasicEntity("Bob") { Population = 300m },
            new BasicEntity("Charlie") { Population = 200m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunAvg", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 200m],
            ["Charlie", 200m]);
    }

    [TestMethod]
    public void WhenFrameWithLeftJoin_ShouldComputeOverJoinedRows()
    {
        var query = @"
            select a.Name, b.City,
                   Sum(a.Population) over (order by a.Name rows between unbounded preceding and current row) as RunSum
            from #A.Entities() a left outer join #B.Entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [
                    new BasicEntity("Alice") { Population = 100m },
                    new BasicEntity("Bob") { Population = 200m },
                    new BasicEntity("Charlie") { Population = 300m }
                ]
            },
            { "#B", [
                    new BasicEntity("Alice") { City = "NYC" },
                    new BasicEntity("Charlie") { City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.City", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 100m],
            ["Bob", null, 300m],
            ["Charlie", "LA", 600m]);
    }

    [TestMethod]
    public void WhenFrameOverUnionCte_ShouldComputeAcrossCombinedRows()
    {
        var query = @"
            with combined as (
                select Name, Population from #A.Entities() where Population > 0
                union all (Name, Population)
                select Name, Population from #A.Entities() where Population > 100
            )
            select Name, Sum(Population) over (order by Name rows between 1 preceding and current row) as RunSum
            from combined";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100m },
            new BasicEntity("Bob") { Population = 200m },
            new BasicEntity("Charlie") { Population = 300m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 300m],
            ["Bob", 400m],
            ["Charlie", 500m],
            ["Charlie", 600m]);
    }

    [TestMethod]
    public void WhenFrameForwardOnly_ShouldComputeCurrentAndFollowing()
    {
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Name rows between current row and 1 following) as FwdSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100m },
            new BasicEntity("Bob") { Population = 200m },
            new BasicEntity("Charlie") { Population = 300m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("FwdSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 300m],
            ["Bob", 200m, 500m],
            ["Charlie", 300m, 300m]);
    }

    [TestMethod]
    public void WhenRangeFrameWithoutOrderBy_ShouldThrowMQ3052()
    {
        const string query = @"
            select Name, Sum(Population) over (
                range between unbounded preceding and current row
            ) as running
            from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100m },
                    new BasicEntity("Bob") { City = "LA", Population = 200m }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy, ex.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenFrameStartAfterEnd_UnboundedFollowingToCurrentRow_ShouldThrowMQ3053()
    {
        const string query = @"
            select Name, Sum(Population) over (
                order by Name
                rows between unbounded following and current row
            ) as running
            from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100m },
                    new BasicEntity("Bob") { City = "LA", Population = 200m }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3053_InvalidWindowFrameBounds, ex.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenFrameStartAfterEnd_CurrentRowToPreceding_ShouldThrowMQ3053()
    {
        const string query = @"
            select Name, Sum(Population) over (
                order by Name
                rows between current row and unbounded preceding
            ) as running
            from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100m },
                    new BasicEntity("Bob") { City = "LA", Population = 200m }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3053_InvalidWindowFrameBounds, ex.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenFrameStartAfterEnd_FollowingToPreceding_ShouldThrowMQ3053()
    {
        const string query = @"
            select Name, Sum(Population) over (
                order by Name
                rows between 3 following and 1 preceding
            ) as running
            from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { City = "NYC", Population = 100m },
                    new BasicEntity("Bob") { City = "LA", Population = 200m }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3053_InvalidWindowFrameBounds, ex.PrimaryEnvelope.Code);
    }
}
