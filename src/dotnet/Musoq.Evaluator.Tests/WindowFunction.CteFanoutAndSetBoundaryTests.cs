using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowFunctionCteFanoutAndSetBoundaryTests : BasicEntityTestBase
{

    [TestMethod]
    public void WhenWindowedCteFansOutToJoinAndMarkConsumers_ShouldReuseStableRanks()
    {
        const string query = @"
            WITH ranked AS (
                SELECT a.Country AS Country, a.City AS City,
                       RowNumber() OVER (
                           PARTITION BY a.Country
                           ORDER BY a.Population DESC, a.City ASC
                       ) AS CityRank
                FROM #A.entities() a
            )
            SELECT leader.Country AS Country, leader.City AS TopCity,
                   r.City AS RunnerUp,
                   EXISTS (
                       SELECT r3.City FROM ranked r3
                       WHERE r3.Country = leader.Country AND r3.CityRank = 3
                   ) AS HasThird
            FROM ranked leader
            LEFT OUTER JOIN ranked r
                ON r.Country = leader.Country AND r.CityRank = 2
            WHERE leader.CityRank = 1
            ORDER BY leader.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Berlin", "DE", 300),
                    new BasicEntity("Hamburg", "DE", 200),
                    new BasicEntity("Munich", "DE", 100),
                    new BasicEntity("Paris", "FR", 180),
                    new BasicEntity("Warsaw", "PL", 250),
                    new BasicEntity("Krakow", "PL", 150)
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("TopCity", typeof(string)),
            ("RunnerUp", typeof(string)),
            ("HasThird", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin", "Hamburg", true],
            ["FR", "Paris", null, false],
            ["PL", "Warsaw", "Krakow", false]);
    }

    [TestMethod]
    public void WhenQualifiedWindowBranchesFeedUnionAndOuterWindow_ShouldPreserveRanksAndDeduplicate()
    {
        const string query = @"
            WITH a_ranked AS (
                SELECT a.Country AS Country, a.City AS City,
                       RowNumber() OVER (
                           PARTITION BY a.Country
                           ORDER BY a.Population DESC, a.City ASC
                       ) AS BranchRank
                FROM #A.entities() a
                QUALIFY RowNumber() OVER (
                    PARTITION BY a.Country
                    ORDER BY a.Population DESC, a.City ASC
                ) <= 2
            ), b_ranked AS (
                SELECT b.Country AS Country, b.City AS City,
                       RowNumber() OVER (
                           PARTITION BY b.Country
                           ORDER BY b.Population DESC, b.City ASC
                       ) AS BranchRank
                FROM #B.entities() b
                QUALIFY RowNumber() OVER (
                    PARTITION BY b.Country
                    ORDER BY b.Population DESC, b.City ASC
                ) <= 2
            ), combined AS (
                SELECT Country, City, BranchRank FROM a_ranked
                UNION (Country, City, BranchRank)
                SELECT Country, City, BranchRank FROM b_ranked
            )
            SELECT c.Country AS Country, c.City AS City,
                   c.BranchRank AS BranchRank,
                   RowNumber() OVER (
                       PARTITION BY c.Country
                       ORDER BY c.BranchRank ASC, c.City ASC
                   ) AS CombinedRank
            FROM combined c
            ORDER BY Country, CombinedRank";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Berlin", "DE", 300),
                    new BasicEntity("Hamburg", "DE", 200),
                    new BasicEntity("Munich", "DE", 100),
                    new BasicEntity("Warsaw", "PL", 300),
                    new BasicEntity("Krakow", "PL", 200),
                    new BasicEntity("Gdansk", "PL", 100)
                ]
            },
            {
                "#B", [
                    new BasicEntity("Munich", "DE", 500),
                    new BasicEntity("Berlin", "DE", 400),
                    new BasicEntity("Warsaw", "PL", 400),
                    new BasicEntity("Lodz", "PL", 250)
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("BranchRank", typeof(long)),
            ("CombinedRank", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin", 1L, 1L],
            ["DE", "Munich", 1L, 2L],
            ["DE", "Berlin", 2L, 3L],
            ["DE", "Hamburg", 2L, 4L],
            ["PL", "Warsaw", 1L, 1L],
            ["PL", "Krakow", 2L, 2L],
            ["PL", "Lodz", 2L, 3L]);
    }
}
