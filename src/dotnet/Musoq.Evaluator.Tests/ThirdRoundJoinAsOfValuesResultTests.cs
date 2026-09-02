using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ThirdRoundJoinAsOfValuesResultTests : BasicEntityTestBase
{

    [TestMethod]
    public void AsOfLeftJoinWithTieBreakAndMissingMatch_ShouldMaterializeCompleteRows()
    {
        const string query = @"
            select a.Name, a.Population, b.Name, b.Population
            from #A.entities() a
            asof left join #B.entities() b on a.Population >= b.Population
            tie break by b.Name desc
            order by a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A1", Population = 100 },
                new BasicEntity { Name = "A2", Population = 10 }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "B-Alpha", Population = 90 },
                new BasicEntity { Name = "B-Zulu", Population = 90 }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Population", typeof(decimal)),
            ("b.Name", typeof(string)),
            ("b.Population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A1", 100m, "B-Zulu", 90m],
            ["A2", 10m, null, null]);
    }

    [TestMethod]
    public void ThreeWayOuterJoinWithDuplicateKeys_ShouldPreserveRowMultiplicity()
    {
        const string query = @"
            select a.Name, b.Name, c.Name
            from #A.entities() a
            left outer join #B.entities() b on a.Country = b.Country
            full outer join #C.entities() c on a.Country = c.Country
            order by a.Name, b.Name, c.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A-DE", Country = "DE" },
                new BasicEntity { Name = "A-FR", Country = "FR" }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "B-DE-1", Country = "DE" },
                new BasicEntity { Name = "B-DE-2", Country = "DE" }
            ],
            ["#C"] =
            [
                new BasicEntity { Name = "C-DE", Country = "DE" },
                new BasicEntity { Name = "C-PL", Country = "PL" }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)),
            ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A-DE", "B-DE-1", "C-DE"],
            ["A-DE", "B-DE-2", "C-DE"],
            ["A-FR", null, null],
            [null, null, "C-PL"]);
    }

    [TestMethod]
    public void ValuesTypedNullsWithWindowAndQualify_ShouldPreserveNullableSchema()
    {
        const string query = @"
            select scores.Name, scores.Score,
                   RowNumber() over (order by scores.Score, scores.Name) as rn
            from values {
                { Name: 'missing', Score: null },
                { Name: 'low', Score: 1 },
                { Name: 'high', Score: 2 }
            } scores
            qualify RowNumber() over (order by scores.Score, scores.Name) <= 2
            order by scores.Score, scores.Name";

        var table = CreateAndRunVirtualMachine(
                query,
                new Dictionary<string, IEnumerable<BasicEntity>>())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Name", typeof(string)),
            ("scores.Score", typeof(int?)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["missing", null, 1L],
            ["low", 1, 2L]);
    }
}
