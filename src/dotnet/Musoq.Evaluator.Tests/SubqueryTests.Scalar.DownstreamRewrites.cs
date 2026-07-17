using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenPartitionedScalarOffsetFeedsOuterWindows_ShouldPreserveValuesAcrossDerivedTable()
    {
        const string query = @"
            SELECT e.City, e.SecondCity,
                   RowNumber() OVER (
                       ORDER BY e.SecondCity ASC NULLS LAST, e.City ASC
                   ) AS PickRank,
                   Lag(e.SecondCity) OVER (
                       ORDER BY e.SecondCity ASC NULLS LAST, e.City ASC
                   ) AS PreviousPick
            FROM (
                SELECT a.City AS City,
                       (
                           SELECT b.City FROM #B.entities() b
                           WHERE b.Country = a.Country
                           ORDER BY b.Population DESC, b.City ASC
                           SKIP 1 TAKE 1
                       ) AS SecondCity
                FROM #A.entities() a
            ) e
            QUALIFY RowNumber() OVER (
                ORDER BY e.SecondCity ASC NULLS LAST, e.City ASC
            ) <= 3
            ORDER BY PickRank";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "Berlin", Country = "DE" },
                    new BasicEntity { City = "Madrid", Country = "ES" },
                    new BasicEntity { City = "Paris", Country = "FR" },
                    new BasicEntity { City = "Warsaw", Country = "PL" }
                ]
            },
            {
                "#B", [
                    new BasicEntity("Munich", "DE", 250),
                    new BasicEntity("Hamburg", "DE", 100),
                    new BasicEntity("Lyon", "FR", 100),
                    new BasicEntity("Gdansk", "PL", 300),
                    new BasicEntity("Krakow", "PL", 200)
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("e.City", typeof(string)),
            ("e.SecondCity", typeof(string)),
            ("PickRank", typeof(long)),
            ("PreviousPick", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "Hamburg", 1L, null],
            ["Warsaw", "Krakow", 2L, "Hamburg"],
            ["Madrid", null, 3L, "Krakow"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarUnionFeedsGroupingAndWindow_ShouldGroupNullableResults()
    {
        const string query = @"
            WITH enriched AS (
                SELECT a.Name AS Person,
                       (
                           SELECT b.City FROM #B.entities() b
                           WHERE b.Country = a.Country
                           UNION (City)
                           SELECT c.City FROM #C.entities() c
                           WHERE c.Country = a.Country
                       ) AS MatchCity
                FROM #A.entities() a
            ), counts AS (
                SELECT e.MatchCity AS MatchCity, Count(e.Person) AS People
                FROM enriched e
                GROUP BY e.MatchCity
            )
            SELECT c.MatchCity AS MatchCity, c.People AS People,
                   RowNumber() OVER (
                       ORDER BY c.People DESC, c.MatchCity ASC NULLS LAST
                   ) AS Rank
            FROM counts c
            ORDER BY Rank";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Alice", Country = "PL" },
                    new BasicEntity { Name = "Bob", Country = "PL" },
                    new BasicEntity { Name = "Claire", Country = "FR" },
                    new BasicEntity { Name = "Dan", Country = "DE" },
                    new BasicEntity { Name = "Eva", Country = "ES" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { City = "Krakow", Country = "PL" },
                    new BasicEntity { City = "Paris", Country = "FR" }
                ]
            },
            {
                "#C", [
                    new BasicEntity { City = "Krakow", Country = "PL" },
                    new BasicEntity { City = "Paris", Country = "FR" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("MatchCity", typeof(string)),
            ("People", typeof(long)),
            ("Rank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Krakow", 2L, 1L],
            [null, 2L, 2L],
            ["Paris", 1L, 3L]);
    }
}
