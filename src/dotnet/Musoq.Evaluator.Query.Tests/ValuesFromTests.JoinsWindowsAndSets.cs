using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_LeftJoinWithSchemaSource_ShouldProduceNullsForMissingPolicies()
    {
        const string query = (
            "\n"
            + "select entity.Name, policy.Approved\n"
            + "from #A.Entities() entity\n"
            + "left outer join values {\n"
            + "    ( Name: 'Newtonsoft.Json', Approved: true ),\n"
            + "    ( Name: 'Legacy.Package', Approved: false )\n"
            + "} policy on entity.Name = policy.Name\n"
            + "order by entity.Name"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Newtonsoft.Json"),
                    new BasicEntity("Legacy.Package"),
                    new BasicEntity("Other.Package"),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Approved", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Legacy.Package", false],
            ["Newtonsoft.Json", true],
            ["Other.Package", null]
        );
    }

    [TestMethod]
    public void ValuesSource_AsLeftJoinInput_ShouldWork()
    {
        const string query = (
            "\n"
            + "select policy.Name, entity.Name\n"
            + "from values {\n"
            + "    ( Name: 'Missing.Package' ),\n"
            + "    ( Name: 'Newtonsoft.Json' )\n"
            + "} policy\n"
            + "left outer join #A.Entities() entity on policy.Name = entity.Name\n"
            + "order by policy.Name"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Newtonsoft.Json")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Missing.Package", null],
            ["Newtonsoft.Json", "Newtonsoft.Json"]
        );
    }

    [TestMethod]
    public void ValuesSource_CrossApply_ShouldReuseLiteralRowsAcrossOuterRows()
    {
        const string query = (
            "\n"
            + "select entity.Name, policy.Flag\n"
            + "from #A.Entities() entity\n"
            + "cross apply values {\n"
            + "    ( Flag: 'A' ),\n"
            + "    ( Flag: 'B' )\n"
            + "} policy\n"
            + "order by entity.Name, policy.Flag"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Newtonsoft.Json"), new BasicEntity("Legacy.Package")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Flag", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Legacy.Package", "A"],
            ["Legacy.Package", "B"],
            ["Newtonsoft.Json", "A"],
            ["Newtonsoft.Json", "B"]
        );
    }

    [TestMethod]
    public void ValuesSource_GroupByHavingOrderBy_ShouldWork()
    {
        const string query = (
            "\n"
            + "select scores.Team, Sum(scores.Score)\n"
            + "from values {\n"
            + "    ( Team: 'red', Score: 2 ),\n"
            + "    ( Team: 'blue', Score: 1 ),\n"
            + "    ( Team: 'red', Score: 3 )\n"
            + "} scores\n"
            + "group by scores.Team\n"
            + "having Sum(scores.Score) > 2\n"
            + "order by Sum(scores.Score) desc"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Team", typeof(string)),
            ("Sum(scores.Score)", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["red", 5]);
    }

    [TestMethod]
    public void ValuesSource_DistinctOrderBy_ShouldWork()
    {
        const string query = (
            "\n"
            + "select distinct scores.Team\n"
            + "from values {\n"
            + "    ( Team: 'red' ),\n"
            + "    ( Team: 'blue' ),\n"
            + "    ( Team: 'red' )\n"
            + "} scores\n"
            + "order by scores.Team"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Team", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["blue"], ["red"]);
    }

    [TestMethod]
    public void ValuesSource_WindowQualify_ShouldWork()
    {
        const string query = (
            "\n"
            + "select scores.Team, scores.Score, RowNumber() over (partition by scores.Team order by scores.Score desc) as rn\n"
            + "from values {\n"
            + "    ( Team: 'red', Score: 2 ),\n"
            + "    ( Team: 'blue', Score: 1 ),\n"
            + "    ( Team: 'red', Score: 3 )\n"
            + "} scores\n"
            + "qualify RowNumber() over (partition by scores.Team order by scores.Score desc) = 1\n"
            + "order by scores.Team"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Team", typeof(string)),
            ("Score", typeof(int)),
            ("rn", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["blue", 1, 1L], ["red", 3, 1L]);
    }

    [TestMethod]
    public void ValuesSource_WindowBuiltInsWithoutSchemaSource_ShouldWork()
    {
        const string query = (
            "\n"
            + "select scores.Name,\n"
            + "       RowNumber() over (order by scores.Score) as rn,\n"
            + "       Rank() over (order by scores.Score) as rnk,\n"
            + "       DenseRank() over (order by scores.Score) as dense_rnk,\n"
            + "       Lag(scores.Score, 1) over (order by scores.Score) as previous_score\n"
            + "from values {\n"
            + "    ( Name: 'first', Score: 10 ),\n"
            + "    ( Name: 'second', Score: 20 ),\n"
            + "    ( Name: 'third', Score: 30 )\n"
            + "} scores\n"
            + "order by scores.Score"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)),
            ("rnk", typeof(long)),
            ("dense_rnk", typeof(long)),
            ("previous_score", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["first", 1L, 1L, 1L, null],
            ["second", 2L, 2L, 2L, 10],
            ["third", 3L, 3L, 3L, 20]
        );
    }

    [TestMethod]
    public void ValuesSource_UnionWithAnotherValuesSource_ShouldWork()
    {
        const string query = (
            "\n"
            + "select packages.Name\n"
            + "from values {\n"
            + "    ( Name: 'Newtonsoft.Json' ),\n"
            + "    ( Name: 'Legacy.Package' )\n"
            + "} packages\n"
            + "union (Name)\n"
            + "select approvals.Name\n"
            + "from values {\n"
            + "    ( Name: 'Legacy.Package' ),\n"
            + "    ( Name: 'Other.Package' )\n"
            + "} approvals"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Legacy.Package"],
            ["Newtonsoft.Json"],
            ["Other.Package"]
        );
    }

    [TestMethod]
    public void ValuesSource_InSubquery_ShouldWork()
    {
        const string query = (
            "\n"
            + "select packages.Name\n"
            + "from values {\n"
            + "    ( Name: 'Newtonsoft.Json' ),\n"
            + "    ( Name: 'Legacy.Package' ),\n"
            + "    ( Name: 'Other.Package' )\n"
            + "} packages\n"
            + "where packages.Name in (\n"
            + "    select approvals.Name\n"
            + "    from values {\n"
            + "        ( Name: 'Legacy.Package' ),\n"
            + "        ( Name: 'Other.Package' )\n"
            + "    } approvals\n"
            + ")\n"
            + "order by packages.Name"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Legacy.Package"], ["Other.Package"]);
    }
}
