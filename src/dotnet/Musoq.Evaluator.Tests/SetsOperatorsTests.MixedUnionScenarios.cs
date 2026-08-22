using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{

    [TestMethod]
    public void MixedSourcesExceptUnionScenarioTest()
    {
        var query =
            @"select Name from #A.Entities()
except (Name)
select Name from #B.Entities()
union (Name)
select Name from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["002"], ["001"]);
    }

    [TestMethod]
    public void MixedSourcesExceptUnionScenario1Test()
    {
        var query =
            @"select Name from #A.Entities()
except (Name)
select Name from #B.Entities()
union (Name)
select Name from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["002"], ["001"]);
    }

    [TestMethod]
    public void MixedSourcesWithSkipExceptUnionWithConditionsScenarioTest()
    {
        var query = @"
with first_slice as (select Name from #A.Entities() skip 1),
second_slice as (select Name from #B.Entities() skip 2),
third_slice as (select Name from #C.Entities() skip 3)
select Name from first_slice
except (Name) select Name from second_slice
union (Name) select Name from third_slice";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["002"]);
    }

    [TestMethod]
    public void MixedSourcesWithSkipIntersectUnionScenarioTest()
    {
        var query = @"
with first_slice as (select Name from #A.Entities() skip 1),
second_slice as (select Name from #B.Entities() skip 2),
third_slice as (select Name from #C.Entities() skip 3)
select Name from first_slice
intersect (Name) select Name from second_slice
union (Name) select Name from third_slice";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("002"), new BasicEntity("001")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            {
                "#C",
                [
                    new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("003"), new BasicEntity("006")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["006"]);
    }

    [TestMethod]
    public void MixedSourcesExceptUnionWithMultipleColumnsScenarioTest()
    {
        var query =
            @"select Name, Population from #A.Entities()
except (Name)
select Name, Population from #B.Entities()
union (Name)
select Name, Population from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["002", 0m],
            ["001", 0m]);
    }

    [TestMethod]
    public void UnionSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
union (City)
select City, Sum(Population) from #B.Entities() group by City
union (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001", "", 100), new BasicEntity("001", "", 100)] },
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            { "#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001", 200m],
            ["003", 39m],
            ["002", 28m]);
    }

    [TestMethod]
    public void UnionAllSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
union all (City)
select City, Sum(Population) from #B.Entities() group by City
union all (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001", "", 100), new BasicEntity("001", "", 100)] },
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            { "#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Sum(Population)", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001", 200m],
            ["003", 39m],
            ["002", 28m]);
    }

}
