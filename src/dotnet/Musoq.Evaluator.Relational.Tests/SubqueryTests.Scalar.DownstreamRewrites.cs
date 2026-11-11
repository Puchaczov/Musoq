using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenPartitionedScalarOffsetFeedsOuterWindows_ShouldPreserveValuesAcrossDerivedTable()
    {
        const string query = (
            "\n"
            + "            SELECT e.City, e.SecondCity,\n"
            + "                   RowNumber() OVER (\n"
            + "                       ORDER BY e.SecondCity ASC NULLS LAST, e.City ASC\n"
            + "                   ) AS PickRank,\n"
            + "                   Lag(e.SecondCity) OVER (\n"
            + "                       ORDER BY e.SecondCity ASC NULLS LAST, e.City ASC\n"
            + "                   ) AS PreviousPick\n"
            + "            FROM (\n"
            + "                SELECT a.City AS City,\n"
            + "                       (\n"
            + "                           SELECT b.City FROM #B.entities() b\n"
            + "                           WHERE b.Country = a.Country\n"
            + "                           ORDER BY b.Population DESC, b.City ASC\n"
            + "                           SKIP 1 TAKE 1\n"
            + "                       ) AS SecondCity\n"
            + "                FROM #A.entities() a\n"
            + "            ) e\n"
            + "            QUALIFY RowNumber() OVER (\n"
            + "                ORDER BY e.SecondCity ASC NULLS LAST, e.City ASC\n"
            + "            ) <= 3\n"
            + "            ORDER BY PickRank"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { City = "Berlin", Country = "DE" },
                    new BasicEntity { City = "Madrid", Country = "ES" },
                    new BasicEntity { City = "Paris", Country = "FR" },
                    new BasicEntity { City = "Warsaw", Country = "PL" },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Munich", "DE", 250),
                    new BasicEntity("Hamburg", "DE", 100),
                    new BasicEntity("Lyon", "FR", 100),
                    new BasicEntity("Gdansk", "PL", 300),
                    new BasicEntity("Krakow", "PL", 200),
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("SecondCity", typeof(string)),
            ("PickRank", typeof(long)),
            ("PreviousPick", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "Hamburg", 1L, null],
            ["Warsaw", "Krakow", 2L, "Hamburg"],
            ["Madrid", null, 3L, "Krakow"]
        );
    }

    [TestMethod]
    public void WhenCorrelatedScalarUnionFeedsGroupingAndWindow_ShouldGroupNullableResults()
    {
        const string query = (
            "\n"
            + "            WITH enriched AS (\n"
            + "                SELECT a.Name AS Person,\n"
            + "                       (\n"
            + "                           SELECT b.City FROM #B.entities() b\n"
            + "                           WHERE b.Country = a.Country\n"
            + "                           UNION (City)\n"
            + "                           SELECT c.City FROM #C.entities() c\n"
            + "                           WHERE c.Country = a.Country\n"
            + "                       ) AS MatchCity\n"
            + "                FROM #A.entities() a\n"
            + "            ), counts AS (\n"
            + "                SELECT e.MatchCity AS MatchCity, Count(e.Person) AS People\n"
            + "                FROM enriched e\n"
            + "                GROUP BY e.MatchCity\n"
            + "            )\n"
            + "            SELECT c.MatchCity AS MatchCity, c.People AS People,\n"
            + "                   RowNumber() OVER (\n"
            + "                       ORDER BY c.People DESC, c.MatchCity ASC NULLS LAST\n"
            + "                   ) AS Rank\n"
            + "            FROM counts c\n"
            + "            ORDER BY Rank"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "Alice", Country = "PL" },
                    new BasicEntity { Name = "Bob", Country = "PL" },
                    new BasicEntity { Name = "Claire", Country = "FR" },
                    new BasicEntity { Name = "Dan", Country = "DE" },
                    new BasicEntity { Name = "Eva", Country = "ES" },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { City = "Krakow", Country = "PL" },
                    new BasicEntity { City = "Paris", Country = "FR" },
                ]
            },
            {
                "#C",
                [
                    new BasicEntity { City = "Krakow", Country = "PL" },
                    new BasicEntity { City = "Paris", Country = "FR" },
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("MatchCity", typeof(string)),
            ("People", typeof(long)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Krakow", 2L, 1L], [null, 2L, 2L], ["Paris", 1L, 3L]);
    }
}
