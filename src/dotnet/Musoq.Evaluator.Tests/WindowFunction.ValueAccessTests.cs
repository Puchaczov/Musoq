using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionValueAccessTests : BasicEntityTestBase
{

    [TestMethod]
    public void WhenFirstValueOverOrderByName_ShouldReturnFirstInPartition()
    {
        var query = "select Name, FirstValue(Name) over (order by Name) as FV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Alice"],
            ["Bob", "Alice"],
            ["Charlie", "Alice"]);
    }

    [TestMethod]
    public void WhenFirstValueWithPartition_ShouldReturnFirstPerPartition()
    {
        var query = @"
            select Name, City, FirstValue(Name) over (partition by City order by Name) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("FV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", "Alice"],
            ["Diana", "LA", "Alice"],
            ["Bob", "NYC", "Bob"],
            ["Charlie", "NYC", "Bob"]);
    }

    [TestMethod]
    public void WhenFirstValueOnNumericColumn_ShouldReturnFirstValue()
    {
        var query = @"
            select Name, FirstValue(Population) over (order by Name) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FV", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 100m],
            ["Bob", 100m],
            ["Charlie", 100m]);
    }

    [TestMethod]
    public void WhenLastValueOverOrderByName_ShouldReturnRunningLast()
    {
        var query = "select Name, LastValue(Name) over (order by Name) as LV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("LV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Alice"],
            ["Bob", "Bob"],
            ["Charlie", "Charlie"]);
    }

    [TestMethod]
    public void WhenLastValueWithPartition_ShouldReturnRunningLastPerPartition()
    {
        var query = @"
            select Name, City, LastValue(Name) over (partition by City order by Name) as LV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("LV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", "Alice"],
            ["Diana", "LA", "Diana"],
            ["Bob", "NYC", "Bob"],
            ["Charlie", "NYC", "Charlie"]);
    }

    [TestMethod]
    public void WhenLastValueWithoutOrderBy_ShouldReturnPartitionLast()
    {
        var query = @"
            select Name, LastValue(Population) over () as LV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { Population = 300 },
            new BasicEntity("Alice") { Population = 100 },
            new BasicEntity("Bob") { Population = 200 });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("LV", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 200m],
            ["Bob", 200m],
            ["Charlie", 200m]);
    }

    [TestMethod]
    public void WhenNthValueWithN2_ShouldReturnSecondValue()
    {
        var query = @"
            select Name, NthValue(Name, 2) over (order by Name) as NV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("NV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null],
            ["Bob", "Bob"],
            ["Charlie", "Bob"]);
    }

    [TestMethod]
    public void WhenNthValueWithPartition_ShouldReturnNthPerPartition()
    {
        var query = @"
            select Name, City, NthValue(Name, 2) over (partition by City order by Name) as NV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("NV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", null],
            ["Diana", "LA", "Diana"],
            ["Bob", "NYC", null],
            ["Charlie", "NYC", "Charlie"]);
    }

    [TestMethod]
    public void WhenNthValueExceedsPartitionSize_ShouldReturnNull()
    {
        var query = @"
            select Name, NthValue(Name, 10) over (order by Name) as NV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Charlie"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("NV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null],
            ["Bob", null],
            ["Charlie", null]);
    }

    [TestMethod]
    public void WhenFirstValueWithUnderscoreSyntax_ShouldWork()
    {
        var query = "select Name, First_Value(Name) over (order by Name) as FV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Alice"],
            ["Bob", "Alice"],
            ["Charlie", "Alice"]);
    }

    [TestMethod]
    public void WhenLastValueWithUnderscoreSyntax_ShouldWork()
    {
        var query = "select Name, Last_Value(Name) over (order by Name) as LV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("LV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Alice"],
            ["Bob", "Bob"],
            ["Charlie", "Charlie"]);
    }

    [TestMethod]
    public void WhenNthValueWithUnderscoreSyntax_ShouldWork()
    {
        var query = "select Name, Nth_Value(Name, 2) over (order by Name) as NV from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("NV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", null],
            ["Bob", "Bob"],
            ["Charlie", "Bob"]);
    }

    [TestMethod]
    public void WhenNthValueWithN1_ShouldBehaveLikeFirstValue()
    {
        var query = @"
            select Name,
                   NthValue(Name, 1) over (order by Name) as NV,
                   FirstValue(Name) over (order by Name) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("NV", typeof(string)),
            ("FV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Alice", "Alice"],
            ["Bob", "Alice", "Alice"],
            ["Charlie", "Alice", "Alice"]);
    }

    [TestMethod]
    public void WhenFirstValueWithFrame_ShouldReturnFirstInFrame()
    {
        var query = @"
            select Name, FirstValue(Name) over (order by Name rows between 1 preceding and 1 following) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Diana"),
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Alice"],
            ["Bob", "Alice"],
            ["Charlie", "Bob"],
            ["Diana", "Charlie"]);
    }

    [TestMethod]
    public void WhenLastValueWithFrame_ShouldReturnLastInFrame()
    {
        var query = @"
            select Name, LastValue(Name) over (order by Name rows between 1 preceding and 1 following) as LV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Diana"),
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("LV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Bob"],
            ["Bob", "Charlie"],
            ["Charlie", "Diana"],
            ["Diana", "Diana"]);
    }

    [TestMethod]
    public void WhenNthValueWithFrame_ShouldReturnNthInFrame()
    {
        var query = @"
            select Name, NthValue(Name, 2) over (order by Name rows between 1 preceding and 1 following) as NV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Diana"),
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("NV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "Bob"],
            ["Bob", "Bob"],
            ["Charlie", "Charlie"],
            ["Diana", "Diana"]);
    }

    [TestMethod]
    public void WhenFirstValueWithUnboundedFrame_ShouldReturnPartitionFirst()
    {
        var query = @"
            select Name, City,
                   FirstValue(Name) over (partition by City order by Name rows between unbounded preceding and unbounded following) as FV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" },
            new BasicEntity("Eve") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("FV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", "Alice"],
            ["Diana", "LA", "Alice"],
            ["Eve", "LA", "Alice"],
            ["Bob", "NYC", "Bob"],
            ["Charlie", "NYC", "Bob"]);
    }

    [TestMethod]
    public void WhenLastValueWithUnboundedFrame_ShouldReturnPartitionLast()
    {
        var query = @"
            select Name, City,
                   LastValue(Name) over (partition by City order by Name rows between unbounded preceding and unbounded following) as LV
            from #A.entities()";

        var sources = CreateSingleSource(
            new BasicEntity("Charlie") { City = "NYC" },
            new BasicEntity("Alice") { City = "LA" },
            new BasicEntity("Bob") { City = "NYC" },
            new BasicEntity("Diana") { City = "LA" },
            new BasicEntity("Eve") { City = "LA" });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("LV", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "LA", "Eve"],
            ["Diana", "LA", "Eve"],
            ["Eve", "LA", "Eve"],
            ["Bob", "NYC", "Charlie"],
            ["Charlie", "NYC", "Charlie"]);
    }
}
