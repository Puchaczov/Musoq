using System.Collections.Generic;
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("entity.Name", typeof(string)),
            ("policy.Approved", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Legacy.Package", false],
            ["Newtonsoft.Json", true],
            ["Other.Package", null]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("policy.Name", typeof(string)),
            ("entity.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Missing.Package", null],
            ["Newtonsoft.Json", "Newtonsoft.Json"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("entity.Name", typeof(string)),
            ("policy.Flag", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Legacy.Package", "A"],
            ["Legacy.Package", "B"],
            ["Newtonsoft.Json", "A"],
            ["Newtonsoft.Json", "B"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Team", typeof(string)),
            ("Sum(scores.Score)", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["red", 5]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("scores.Team", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["blue"], ["red"]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Team", typeof(string)),
            ("scores.Score", typeof(int)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["blue", 1, 1L],
            ["red", 3, 1L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Name", typeof(string)),
            ("rn", typeof(long)),
            ("rnk", typeof(long)),
            ("dense_rnk", typeof(long)),
            ("previous_score", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["first", 1L, 1L, 1L, null],
            ["second", 2L, 2L, 2L, 10],
            ["third", 3L, 3L, 3L, 20]);
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
        TableMaterializationTestHelper.AssertColumns(table, ("packages.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Legacy.Package"],
            ["Newtonsoft.Json"],
            ["Other.Package"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("packages.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Legacy.Package"],
            ["Other.Package"]);
    }

}
