using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class NonEquiJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void SimpleNonEquiJoinTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 150 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void MultiJoinNonEquiTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name,
    c.Name
from #A.entities() a 
inner join #B.entities() b on a.Population > b.Population
inner join #C.entities() c on b.Population > c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 300 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 200 },
                    new BasicEntity { Name = "B2", Population = 400 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 100 },
                    new BasicEntity { Name = "C2", Population = 250 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
    }

    [TestMethod]
    public void LessThanTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population < b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 150 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B2", table[0][1]);
    }

    [TestMethod]
    public void GreaterOrEqualTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 150 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void LessOrEqualTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population <= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 150 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void NotEqualTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Population <> b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 50 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B2", table[0][1]);
    }

    [TestMethod]
    public void DateComparisonTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Time > b.Time";

        var now = DateTime.Now;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Time = now }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Time = now.AddDays(-1) },
                    new BasicEntity { Name = "B2", Time = now.AddDays(1) }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
    }

    [TestMethod]
    public void MixedEquiAndNonEquiTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on a.Country = b.Country AND a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "PL", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "DE", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "PL", Population = 50 },
                    new BasicEntity { Name = "B2", Country = "PL", Population = 150 },
                    new BasicEntity { Name = "B3", Country = "DE", Population = 50 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void ComplexConditionsTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b on (a.Population > b.Population OR a.Money > b.Money)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100, Money = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50, Money = 200 },
                    new BasicEntity { Name = "B2", Population = 150, Money = 50 },
                    new BasicEntity { Name = "B3", Population = 150, Money = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void LeftJoinNonEquiTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left join #B.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.Select(r => (string)r[0]).ToList();
        CollectionAssert.Contains(rows, "A1");
        CollectionAssert.Contains(rows, "A2");

        var a1Row = table.Single(r => (string)r[0] == "A1");
        Assert.AreEqual("B1", a1Row[1]);

        var a2Row = table.Single(r => (string)r[0] == "A2");
        Assert.IsNull(a2Row[1]);
    }

    [TestMethod]
    public void SelfJoinNonEquiTest()
    {
        var query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #A.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "High", Population = 100 },
                    new BasicEntity { Name = "Low", Population = 50 }
                ]
            }
        };


        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("High", table[0][0]);
        Assert.AreEqual("Low", table[0][1]);
    }
}