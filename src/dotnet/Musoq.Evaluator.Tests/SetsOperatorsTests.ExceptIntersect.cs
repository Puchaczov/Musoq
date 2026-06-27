using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{

    [TestMethod]
    public void ExceptDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() except (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("002", table[0].Values[0]);
    }

    [TestMethod]
    public void ExceptWithSkipDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() skip 1 except (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("010")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("010", table[0].Values[0]);
    }

    [TestMethod]
    public void ExceptTripleSourcesTest()
    {
        var query =
            @"select Name from #A.Entities() except (Name) select Name from #B.Entities() except (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ExceptWithSkipTripleSourcesTest()
    {
        var query =
            @"select Name from #A.Entities() skip 1 except (Name)
select Name from #B.Entities() skip 2 except (Name)
select Name from #C.Entities() skip 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("002", table[0].Values[0]);
    }

    [TestMethod]
    public void ExceptMultipleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() except (Name)
select Name from #B.Entities() except (Name)
select Name from #C.Entities() except (Name)
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                ]
            },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table.Count(r => (string)r.Values[0] == "002"), "Expected one row with '002'");
        Assert.AreEqual(1, table.Count(r => (string)r.Values[0] == "008"), "Expected one row with '008'");
    }

    [TestMethod]
    public void IntersectDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectWithSkipDoubleSourceTest()
    {
        var query = @"select Name from #A.Entities() skip 1 intersect (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
            {
                "#B",
                [
                    new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("005", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectTripleSourcesTest()
    {
        var query =
            @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities() intersect (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectWithSkipTripleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() skip 1 intersect (Name)
select Name from #B.Entities() skip 2 intersect (Name)
select Name from #C.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
            {
                "#B",
                [
                    new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001"), new BasicEntity("005")
                ]
            },
            { "#C", [new BasicEntity("002"), new BasicEntity("001"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("005", table[0].Values[0]);
    }

    [TestMethod]
    public void IntersectMultipleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() intersect (Name)
select Name from #B.Entities() intersect (Name)
select Name from #C.Entities() intersect (Name)
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                ]
            },
            { "#B", [new BasicEntity("003"), new BasicEntity("007"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("005"), new BasicEntity("007"), new BasicEntity("001")] },
            { "#D", [new BasicEntity("008"), new BasicEntity("007"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "007"), "Second entry should be '007'");
    }

    [TestMethod]
    public void ExceptSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                    new BasicEntity("002", "", 500)
                ]
            },
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

        Assert.AreEqual(1, table.Count, "Table should have 1 entry");

        Assert.IsTrue(table.Any(entry =>
            (string)entry.Values[0] == "001" &&
            (decimal)entry.Values[1] == 200m
        ), "First entry should be '001' with value 200");
    }

    [TestMethod]
    public void IntersectSourceGroupByTest()
    {
        var query =
            @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                    new BasicEntity("002", "", 500)
                ]
            },
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.AreEqual(200m, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenExceptHasEmptyKeyList_ShouldUseAllProjectedFields()
    {
        var query = @"select Name from #A.Entities() except () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenIntersectHasEmptyKeyList_ShouldUseAllProjectedFields()
    {
        var query = @"select Name from #A.Entities() intersect () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

}
