using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AsOfJoinTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenAsOfJoinWithGreaterOrEqual_ShouldReturnClosestMatch()
    {
        var query = @"
select 
    a.Name, 
    a.Population,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 },
                    new BasicEntity { Name = "A3", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 40 },
                    new BasicEntity { Name = "B3", Population = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[1]).ToList();

        // A1 (100) >= B1 (90) — closest match
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (50) >= B2 (40) — closest match
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][2]);

        // A3 (10) >= B3 (5) — closest match
        Assert.AreEqual("A3", rows[2][0]);
        Assert.AreEqual("B3", rows[2][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithGreaterThan_ShouldReturnStrictlyLessMatch()
    {
        var query = @"
select 
    a.Name, 
    a.Population,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[1]).ToList();

        // A1 (100) > B2 (50) — closest strictly less
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B2", rows[0][2]);

        // A2 (50) > B3 (30) — closest strictly less
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B3", rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithLessThanOrEqual_ShouldReturnClosestGreaterOrEqual()
    {
        var query = @"
select 
    a.Name, 
    a.Population,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population <= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 20 },
                    new BasicEntity { Name = "B2", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[1]).ToList();

        // A1 (10) <= B1 (20) — smallest right >= left
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (50) <= B2 (60) — smallest right >= left
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithLessThan_ShouldReturnClosestStrictlyGreater()
    {
        var query = @"
select 
    a.Name, 
    a.Population,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population < b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[1]).ToList();

        // A1 (10) < B2 (50) — smallest right strictly > left
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B2", rows[0][2]);

        // A2 (50) < B3 (60) — smallest right strictly > left
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B3", rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinNoMatch_ShouldReturnEmpty()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinNoMatch_ShouldReturnLeftWithNulls()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithMatch_ShouldReturnClosestMatch()
    {
        var query = @"
select 
    a.Name, 
    a.Population,
    b.Name,
    b.Population
from #A.entities() a 
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[1]).ToList();

        // A1 (100) >= B1 (50) — match
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (1) — no B <= 1, null
        Assert.AreEqual("A2", rows[1][0]);
        Assert.IsNull(rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithEqualityAndInequality_ShouldPartitionByEqualityKey()
    {
        var query = @"
select 
    a.Name,
    a.Country,
    a.Population,
    b.Name,
    b.Country,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Country = b.Country and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "UK", Population = 80 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 90 },
                    new BasicEntity { Name = "B2", Country = "US", Population = 50 },
                    new BasicEntity { Name = "B3", Country = "UK", Population = 70 },
                    new BasicEntity { Name = "B4", Country = "UK", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[2]).ToList();

        // A1 (US, 100) matched with B1 (US, 90) — closest US match
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][3]);

        // A2 (UK, 80) matched with B3 (UK, 70) — closest UK match
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B3", rows[1][3]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithEqualityNoMatch_ShouldReturnNulls()
    {
        var query = @"
select 
    a.Name,
    a.Country,
    b.Name
from #A.entities() a 
asof left join #B.entities() b on a.Country = b.Country and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "FR", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // A1 (US, 100) matches B1 (US, 90)
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (FR, 50) — no FR in B, null
        Assert.AreEqual("A2", rows[1][0]);
        Assert.IsNull(rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinExactMatch_ShouldReturnExactMatch()
    {
        var query = @"
select 
    a.Name, 
    b.Name,
    a.Population,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        // A1 (50) >= B1 (50) — exact match preferred
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfLeftOuterJoin_ShouldWorkSameAsAsOfLeftJoin()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof left outer join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinEmptyRight_ShouldReturnEmpty()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinEmptyRight_ShouldReturnLeftWithNulls()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithDateTimeColumn_ShouldMatchByTime()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Time >= b.Time";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Error1", Time = new DateTime(2025, 1, 15, 14, 30, 0) },
                    new BasicEntity { Name = "Error2", Time = new DateTime(2025, 1, 15, 10, 0, 0) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Commit1", Time = new DateTime(2025, 1, 15, 14, 0, 0) },
                    new BasicEntity { Name = "Commit2", Time = new DateTime(2025, 1, 15, 9, 0, 0) },
                    new BasicEntity { Name = "Commit3", Time = new DateTime(2025, 1, 14, 12, 0, 0) }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("Error1", rows[0][0]);
        Assert.AreEqual("Commit1", rows[0][1]);

        Assert.AreEqual("Error2", rows[1][0]);
        Assert.AreEqual("Commit2", rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithStringColumn_ShouldMatchLexicographically()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.City >= b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", City = "M" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", City = "A" },
                    new BasicEntity { Name = "B2", City = "K" },
                    new BasicEntity { Name = "B3", City = "Z" }
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
    public void WhenAsOfJoinWithWhereClause_ShouldFilterAfterJoin()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population
where a.Name <> 'A2'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 40 }
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
    public void WhenAsOfJoinWithGroupBy_ShouldAggregateAfterJoin()
    {
        var query = @"
select 
    a.Country,
    Count(a.Name)
from #A.entities() a 
asof join #B.entities() b on a.Country = b.Country and a.Population >= b.Population
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "US", Population = 80 },
                    new BasicEntity { Name = "A3", Country = "UK", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 70 },
                    new BasicEntity { Name = "B2", Country = "UK", Population = 40 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("UK", rows[0][0]);
        Assert.AreEqual(1, (int)rows[0][1]);

        Assert.AreEqual("US", rows[1][0]);
        Assert.AreEqual(2, (int)rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithOrderByAndTake_ShouldApplyAfterJoin()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population
order by a.Name desc
take 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 80 },
                    new BasicEntity { Name = "A3", Population = 60 }
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
        Assert.AreEqual("A3", table[0][0]);
        Assert.AreEqual("A2", table[1][0]);
    }

    [TestMethod]
    public void WhenAsOfJoinLeftSideEmpty_ShouldReturnEmpty()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", Array.Empty<BasicEntity>()
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenAsOfJoinBothSidesEmpty_ShouldReturnEmpty()
    {
        var query = @"
select 
    a.Name, 
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", Array.Empty<BasicEntity>()
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenAsOfJoinWithCte_ShouldWorkCorrectly()
    {
        var query = @"
with leftCte as (
    select Name, Population from #A.entities()
),
rightCte as (
    select Name, Population from #B.entities()
)
select l.Name, r.Name 
from leftCte l 
asof join rightCte r on l.Population >= r.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 50 }
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
    public void WhenAsOfJoinChainedWithInnerJoin_ShouldWorkCorrectly()
    {
        var query = @"
select 
    a.Name,
    b.Name,
    c.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population
inner join #C.entities() c on a.Country = c.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100, Country = "US" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Country = "US" }
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
    public void WhenAsOfJoinWithMultipleEqualityKeys_ShouldPartitionByCompositeKey()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof join #B.entities() b on a.Country = b.Country and a.City = b.City and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", City = "NYC", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "US", City = "LA", Population = 80 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", City = "NYC", Population = 90 },
                    new BasicEntity { Name = "B2", Country = "US", City = "LA", Population = 70 },
                    new BasicEntity { Name = "B3", Country = "US", City = "NYC", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithPartitionColumn_ShouldCorrelatePerService()
    {
        var query = @"
select 
    errors.Name,
    errors.Time,
    deploys.Name,
    deploys.Time
from #A.entities() errors 
asof join #B.entities() deploys on errors.Country = deploys.Country and errors.Time >= deploys.Time";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Error-Auth", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 14, 30, 0) },
                    new BasicEntity { Name = "Error-Pay",  Country = "pay-svc",  Time = new DateTime(2025, 3, 10, 15, 0, 0) },
                    new BasicEntity { Name = "Error-Auth2", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 10, 0, 0) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Deploy-Auth-v2", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 14, 0, 0) },
                    new BasicEntity { Name = "Deploy-Auth-v1", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 9, 0, 0) },
                    new BasicEntity { Name = "Deploy-Pay-v1",  Country = "pay-svc",  Time = new DateTime(2025, 3, 10, 12, 0, 0) }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // Error-Auth (auth-svc, 14:30) -> Deploy-Auth-v2 (auth-svc, 14:00) — most recent deploy before error
        Assert.AreEqual("Error-Auth", rows[0][0]);
        Assert.AreEqual("Deploy-Auth-v2", rows[0][2]);

        // Error-Auth2 (auth-svc, 10:00) -> Deploy-Auth-v1 (auth-svc, 9:00) — only deploy before this error
        Assert.AreEqual("Error-Auth2", rows[1][0]);
        Assert.AreEqual("Deploy-Auth-v1", rows[1][2]);

        // Error-Pay (pay-svc, 15:00) -> Deploy-Pay-v1 (pay-svc, 12:00) — only pay-svc deploy
        Assert.AreEqual("Error-Pay", rows[2][0]);
        Assert.AreEqual("Deploy-Pay-v1", rows[2][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinInequalityReferencesOneSide_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Population >= a.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 50m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenAsOfJoinWithOrCondition_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population or a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3038_AsOfJoinOrNotSupported, DiagnosticPhase.Bind, "OR");
    }

    [TestMethod]
    public void WhenAsOfJoinWithMultipleInequalities_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population and a.Money > b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 1m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 2m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenAsOfJoinWithNoInequality_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1" }] },
            { "#B", [new BasicEntity { Name = "B1" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3036_AsOfJoinMissingInequality, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenAsOfJoinWithReversedOperandOrder_ShouldSwapAndMatch()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof join #B.entities() b on b.Population >= a.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 20 },
                    new BasicEntity { Name = "B2", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // b.Population >= a.Population: find smallest right key >= left probe
        // A1 (10) -> B1 (20) — smallest B >= 10
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        // A2 (50) -> B2 (60) — smallest B >= 50
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithDuplicateRightKeys_ShouldPickOne()
    {
        var query = @"
select 
    a.Name,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 90 },
                    new BasicEntity { Name = "B3", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        // A1 (100) should match one of B1/B2 (both 90) — the closest keys
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual(90m, (decimal)table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithNullInequalityKey_ShouldReturnNulls()
    {
        var query = @"
select 
    a.Name,
    b.Name
from #A.entities() a 
asof left join #B.entities() b on a.NullableValue >= b.NullableValue";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", NullableValue = null },
                    new BasicEntity { Name = "A2", NullableValue = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", NullableValue = 30 },
                    new BasicEntity { Name = "B2", NullableValue = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // A2 (50) >= B1 (30) — match
        var a2Row = rows.First(r => (string)r[0] == "A2");
        Assert.AreEqual("B1", a2Row[1]);

        // A1 (null) — null probe should not match, left join returns nulls
        var a1Row = rows.First(r => (string)r[0] == "A1");
        Assert.IsNull(a1Row[1]);
    }

    [TestMethod]
    public void WhenAsOfJoinChainedWithAsOfJoin_ShouldWorkCorrectly()
    {
        var query = @"
select 
    a.Name,
    b.Name,
    c.Name
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population
asof join #C.entities() c on a.Population >= c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 80 }
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
    public void WhenAsOfJoinGreaterThanWithDuplicateExactKeys_ShouldSkipAllDuplicates()
    {
        var query = @"
select 
    a.Name,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 50 },
                    new BasicEntity { Name = "B4", Population = 50 },
                    new BasicEntity { Name = "B5", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual(10m, (decimal)table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinLessThanWithDuplicateExactKeys_ShouldSkipAllDuplicates()
    {
        var query = @"
select 
    a.Name,
    b.Name,
    b.Population
from #A.entities() a 
asof join #B.entities() b on a.Population < b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 50 },
                    new BasicEntity { Name = "B4", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual(90m, (decimal)table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinInequalityReferencesOnlyRightSide_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on b.Population >= b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 50m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, DiagnosticPhase.Bind, "both sides");
    }

    [TestMethod]
    public void WhenAsOfJoinWithOrNestedInsideAnd_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Name = b.Name and (a.City = b.City or a.Country = b.Country)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", City = "C1", Country = "US" }] },
            { "#B", [new BasicEntity { Name = "B1", City = "C2", Country = "UK" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3038_AsOfJoinOrNotSupported, DiagnosticPhase.Bind, "OR");
    }

    [TestMethod]
    public void WhenAsOfJoinWithMultipleEqualitiesAndNoInequality_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Name = b.Name and a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", City = "C1" }] },
            { "#B", [new BasicEntity { Name = "B1", City = "C1" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3036_AsOfJoinMissingInequality, DiagnosticPhase.Bind, "inequality");
    }

    [TestMethod]
    public void WhenAsOfJoinWithMixedInequalityOperators_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof join #B.entities() b on a.Population >= b.Population and a.Money <= b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 1m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 2m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities, DiagnosticPhase.Bind, "exactly one");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinInequalityReferencesOneSide_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof left join #B.entities() b on a.Population >= a.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 50m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, DiagnosticPhase.Bind, "both sides");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithOrCondition_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof left join #B.entities() b on a.Population >= b.Population or a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3038_AsOfJoinOrNotSupported, DiagnosticPhase.Bind, "OR");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithMultipleInequalities_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof left join #B.entities() b on a.Population >= b.Population and a.Money > b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 1m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 2m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities, DiagnosticPhase.Bind, "exactly one");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithNoInequality_ShouldThrow()
    {
        var query = @"
select a.Name 
from #A.entities() a 
asof left join #B.entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1" }] },
            { "#B", [new BasicEntity { Name = "B1" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3036_AsOfJoinMissingInequality, DiagnosticPhase.Bind, "inequality");
    }
}
