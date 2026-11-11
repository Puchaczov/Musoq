using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ThirdRoundJoinAsOfValuesResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void AsOfLeftJoinWithTieBreakAndMissingMatch_ShouldMaterializeCompleteRows()
    {
        const string query = (
            "\n"
            + "            select a.Name, a.Population, b.Name, b.Population\n"
            + "            from #A.entities() a\n"
            + "            asof left join #B.entities() b on a.Population >= b.Population\n"
            + "            tie break by b.Name desc\n"
            + "            order by a.Name"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A1", Population = 100 },
                new BasicEntity { Name = "A2", Population = 10 },
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "B-Alpha", Population = 90 },
                new BasicEntity { Name = "B-Zulu", Population = 90 },
            ],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Population", typeof(decimal)),
            ("Name", typeof(string)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A1", 100m, "B-Zulu", 90m], ["A2", 10m, null, null]);
    }

    [TestMethod]
    public void ThreeWayOuterJoinWithDuplicateKeys_ShouldPreserveRowMultiplicity()
    {
        const string query = (
            "\n"
            + "            select a.Name, b.Name, c.Name\n"
            + "            from #A.entities() a\n"
            + "            left outer join #B.entities() b on a.Country = b.Country\n"
            + "            full outer join #C.entities() c on a.Country = c.Country\n"
            + "            order by a.Name, b.Name, c.Name"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A-DE", Country = "DE" },
                new BasicEntity { Name = "A-FR", Country = "FR" },
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "B-DE-1", Country = "DE" },
                new BasicEntity { Name = "B-DE-2", Country = "DE" },
            ],
            ["#C"] =
            [
                new BasicEntity { Name = "C-DE", Country = "DE" },
                new BasicEntity { Name = "C-PL", Country = "PL" },
            ],
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Name", typeof(string)),
            ("Name", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A-DE", "B-DE-1", "C-DE"],
            ["A-DE", "B-DE-2", "C-DE"],
            ["A-FR", null, null],
            [null, null, "C-PL"]
        );
    }

    [TestMethod]
    public void ValuesTypedNullsWithWindowAndQualify_ShouldPreserveNullableSchema()
    {
        const string query = (
            "\n"
            + "            select scores.Name, scores.Score,\n"
            + "                   RowNumber() over (order by scores.Score, scores.Name) as rn\n"
            + "            from values {\n"
            + "                ( Name: 'missing', Score: null ),\n"
            + "                ( Name: 'low', Score: 1 ),\n"
            + "                ( Name: 'high', Score: 2 )\n"
            + "            } scores\n"
            + "            qualify RowNumber() over (order by scores.Score, scores.Name) <= 2\n"
            + "            order by scores.Score, scores.Name"
        );

        var table = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Score", typeof(int?)),
            ("rn", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["missing", null, 1L], ["low", 1, 2L]);
    }
}
