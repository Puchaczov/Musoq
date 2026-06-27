using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_LeftJoinWithSchemaSource_ShouldProduceNullsForMissingPolicies()
    {
        const string query = @"
select entity.Name, policy.Approved
from #A.Entities() entity
left outer join values {
    { Name: 'Newtonsoft.Json', Approved: true },
    { Name: 'Legacy.Package', Approved: false }
} policy on entity.Name = policy.Name
order by entity.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Newtonsoft.Json"),
                    new BasicEntity("Legacy.Package"),
                    new BasicEntity("Other.Package")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Legacy.Package", table[0][0]);
        Assert.IsFalse((bool)table[0][1]);
        Assert.AreEqual("Newtonsoft.Json", table[1][0]);
        Assert.IsTrue((bool)table[1][1]);
        Assert.AreEqual("Other.Package", table[2][0]);
        Assert.IsNull(table[2][1]);
    }

    [TestMethod]
    public void ValuesSource_AsLeftJoinInput_ShouldWork()
    {
        const string query = @"
select policy.Name, entity.Name
from values {
    { Name: 'Missing.Package' },
    { Name: 'Newtonsoft.Json' }
} policy
left outer join #A.Entities() entity on policy.Name = entity.Name
order by policy.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Newtonsoft.Json")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Missing.Package", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("Newtonsoft.Json", table[1][0]);
        Assert.AreEqual("Newtonsoft.Json", table[1][1]);
    }

    [TestMethod]
    public void ValuesSource_CrossApply_ShouldReuseLiteralRowsAcrossOuterRows()
    {
        const string query = @"
select entity.Name, policy.Flag
from #A.Entities() entity
cross apply values {
    { Flag: 'A' },
    { Flag: 'B' }
} policy
order by entity.Name, policy.Flag";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Newtonsoft.Json"), new BasicEntity("Legacy.Package")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Legacy.Package", table[0][0]);
        Assert.AreEqual("A", table[0][1]);
        Assert.AreEqual("Legacy.Package", table[1][0]);
        Assert.AreEqual("B", table[1][1]);
        Assert.AreEqual("Newtonsoft.Json", table[2][0]);
        Assert.AreEqual("A", table[2][1]);
        Assert.AreEqual("Newtonsoft.Json", table[3][0]);
        Assert.AreEqual("B", table[3][1]);
    }

    [TestMethod]
    public void ValuesSource_GroupByHavingOrderBy_ShouldWork()
    {
        const string query = @"
select scores.Team, Sum(scores.Score)
from values {
    { Team: 'red', Score: 2 },
    { Team: 'blue', Score: 1 },
    { Team: 'red', Score: 3 }
} scores
group by scores.Team
having Sum(scores.Score) > 2
order by Sum(scores.Score) desc";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("red", table[0][0]);
        Assert.AreEqual(5, table[0][1]);
    }

    [TestMethod]
    public void ValuesSource_DistinctOrderBy_ShouldWork()
    {
        const string query = @"
select distinct scores.Team
from values {
    { Team: 'red' },
    { Team: 'blue' },
    { Team: 'red' }
} scores
order by scores.Team";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("blue", table[0][0]);
        Assert.AreEqual("red", table[1][0]);
    }

    [TestMethod]
    public void ValuesSource_WindowQualify_ShouldWork()
    {
        const string query = @"
select scores.Team, scores.Score, RowNumber() over (partition by scores.Team order by scores.Score desc) as rn
from values {
    { Team: 'red', Score: 2 },
    { Team: 'blue', Score: 1 },
    { Team: 'red', Score: 3 }
} scores
qualify RowNumber() over (partition by scores.Team order by scores.Score desc) = 1
order by scores.Team";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("blue", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual("red", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(1L, table[1][2]);
    }

    [TestMethod]
    public void ValuesSource_WindowBuiltInsWithoutSchemaSource_ShouldWork()
    {
        const string query = @"
select scores.Name,
       RowNumber() over (order by scores.Score) as rn,
       Rank() over (order by scores.Score) as rnk,
       DenseRank() over (order by scores.Score) as dense_rnk,
       Lag(scores.Score, 1) over (order by scores.Score) as previous_score
from values {
    { Name: 'first', Score: 10 },
    { Name: 'second', Score: 20 },
    { Name: 'third', Score: 30 }
} scores
order by scores.Score";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(1L, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.IsNull(table[0][4]);
        Assert.AreEqual("second", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual(2L, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
        Assert.AreEqual(10, table[1][4]);
        Assert.AreEqual("third", table[2][0]);
        Assert.AreEqual(3L, table[2][1]);
        Assert.AreEqual(3L, table[2][2]);
        Assert.AreEqual(3L, table[2][3]);
        Assert.AreEqual(20, table[2][4]);
    }

    [TestMethod]
    public void ValuesSource_UnionWithAnotherValuesSource_ShouldWork()
    {
        const string query = @"
select packages.Name
from values {
    { Name: 'Newtonsoft.Json' },
    { Name: 'Legacy.Package' }
} packages
union (Name)
select approvals.Name
from values {
    { Name: 'Legacy.Package' },
    { Name: 'Other.Package' }
} approvals";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);
        var names = new HashSet<string>
        {
            (string)table[0][0],
            (string)table[1][0],
            (string)table[2][0]
        };

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEquivalent(
            new[] { "Legacy.Package", "Newtonsoft.Json", "Other.Package" },
            names.ToArray());
    }

    [TestMethod]
    public void ValuesSource_InSubquery_ShouldWork()
    {
        const string query = @"
select packages.Name
from values {
    { Name: 'Newtonsoft.Json' },
    { Name: 'Legacy.Package' },
    { Name: 'Other.Package' }
} packages
where packages.Name in (
    select approvals.Name
    from values {
        { Name: 'Legacy.Package' },
        { Name: 'Other.Package' }
    } approvals
)
order by packages.Name";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Legacy.Package", table[0][0]);
        Assert.AreEqual("Other.Package", table[1][0]);
    }

}
