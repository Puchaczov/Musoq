using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Second entry should be '001'");
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

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Second entry should be '001'");
    }

    [TestMethod]
    public void MixedSourcesWithSkipExceptUnionWithConditionsScenarioTest()
    {
        var query =
            @"select Name from #A.Entities() skip 1
except (Name)
select Name from #B.Entities() skip 2
union (Name)
select Name from #C.Entities() skip 3";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("002", table[0].Values[0]);
    }

    [TestMethod]
    public void MixedSourcesWithSkipIntersectUnionScenarioTest()
    {
        var query =
            @"select Name from #A.Entities() skip 1
intersect (Name)
select Name from #B.Entities() skip 2
union (Name)
select Name from #C.Entities() skip 3";

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

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "006"), "Second entry should be '006'");
    }

    [TestMethod]
    public void MixedSourcesExceptUnionWithMultipleColumnsScenarioTest()
    {
        var query =
            @"select Name, RandomNumber() from #A.Entities()
except (Name)
select Name, RandomNumber() from #B.Entities()
union (Name)
select Name, RandomNumber() from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Second entry should be '001'");
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

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "001" &&
            (decimal)entry.Values[1] == 200m
        ), "First entry should be '001' with value 200");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "003" &&
            (decimal)entry.Values[1] == 39m
        ), "Second entry should be '003' with value 39");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "002" &&
            (decimal)entry.Values[1] == 28m
        ), "Third entry should be '002' with value 28");
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

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "001" &&
            (decimal)entry.Values[1] == 200m
        ), "First entry should be '001' with value 200");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "003" &&
            (decimal)entry.Values[1] == 39m
        ), "Second entry should be '003' with value 39");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "002" &&
            (decimal)entry.Values[1] == 28m
        ), "Third entry should be '002' with value 28");
    }

}
