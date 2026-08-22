using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    #region Implicit Frame Defaults

    [TestMethod]
    public void WhenOrderByWithoutFrame_ShouldDefaultToRunningSum()
    {
        // Default with ORDER BY: RANGE UNBOUNDED PRECEDING to CURRENT ROW
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Name) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 100m],
            ["Bob", 200m, 300m],
            ["Charlie", 300m, 600m]);
    }

    [TestMethod]
    public void WhenOrderByWithoutFrame_WithTiedValues_ShouldIncludeCompletePeerGroup()
    {
        const string query = @"
            select Name, Population,
                   Sum(Population) over (order by Population) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 200m],
            ["Bob", 100m, 200m],
            ["Charlie", 200m, 400m]);
    }

    [TestMethod]
    public void WhenNoOrderByNoFrame_ShouldReturnWholePartitionSum()
    {
        // Default without ORDER BY: ROWS UNBOUNDED PRECEDING to UNBOUNDED FOLLOWING
        var query = @"
            select Name, Population,
                   Sum(Population) over () as TotalSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 },
            new BasicEntity("Charlie") { Population = 300 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("TotalSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 600m],
            ["Bob", 200m, 600m],
            ["Charlie", 300m, 600m]);
    }

    [TestMethod]
    public void WhenRowsWithTiedValues_ShouldAccumulatePerRow()
    {
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Population rows between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 100m],
            ["Bob", 100m, 200m],
            ["Charlie", 200m, 400m]);
    }

    [TestMethod]
    [FeatureEvidence("range-window-frames", FeatureEvidenceKind.RuntimePositive)]
    public void WhenRangeWithTiedValues_ShouldIncludeCompletePeerGroup()
    {
        var query = @"
            select Name, Population,
                   Sum(Population) over (order by Population range between unbounded preceding and current row) as RunSum
            from #A.Entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 100 },
            new BasicEntity("Charlie") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("RunSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m, 200m],
            ["Bob", 100m, 200m],
            ["Charlie", 200m, 400m]);
    }

    #endregion
}
