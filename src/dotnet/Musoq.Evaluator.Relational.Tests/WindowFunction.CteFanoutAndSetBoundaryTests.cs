using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class WindowFunctionCteFanoutAndSetBoundaryTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenWindowedCteFansOutToJoinAndMarkConsumers_ShouldReuseStableRanks()
    {
        const string query = (
            "\n"
            + "            WITH ranked AS (\n"
            + "                SELECT a.Country AS Country, a.City AS City,\n"
            + "                       RowNumber() OVER (\n"
            + "                           PARTITION BY a.Country\n"
            + "                           ORDER BY a.Population DESC, a.City ASC\n"
            + "                       ) AS CityRank\n"
            + "                FROM #A.entities() a\n"
            + "            )\n"
            + "            SELECT leader.Country AS Country, leader.City AS TopCity,\n"
            + "                   r.City AS RunnerUp,\n"
            + "                   EXISTS (\n"
            + "                       SELECT r3.City FROM ranked r3\n"
            + "                       WHERE r3.Country = leader.Country AND r3.CityRank = 3\n"
            + "                   ) AS HasThird\n"
            + "            FROM ranked leader\n"
            + "            LEFT OUTER JOIN ranked r\n"
            + "                ON r.Country = leader.Country AND r.CityRank = 2\n"
            + "            WHERE leader.CityRank = 1\n"
            + "            ORDER BY leader.Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Berlin", "DE", 300),
                    new BasicEntity("Hamburg", "DE", 200),
                    new BasicEntity("Munich", "DE", 100),
                    new BasicEntity("Paris", "FR", 180),
                    new BasicEntity("Warsaw", "PL", 250),
                    new BasicEntity("Krakow", "PL", 150),
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("TopCity", typeof(string)),
            ("RunnerUp", typeof(string)),
            ("HasThird", typeof(bool))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin", "Hamburg", true],
            ["FR", "Paris", null, false],
            ["PL", "Warsaw", "Krakow", false]
        );
    }

    [TestMethod]
    public void WhenQualifiedWindowBranchesFeedUnionAndOuterWindow_ShouldPreserveRanksAndDeduplicate()
    {
        const string query = (
            "\n"
            + "            WITH a_ranked AS (\n"
            + "                SELECT a.Country AS Country, a.City AS City,\n"
            + "                       RowNumber() OVER (\n"
            + "                           PARTITION BY a.Country\n"
            + "                           ORDER BY a.Population DESC, a.City ASC\n"
            + "                       ) AS BranchRank\n"
            + "                FROM #A.entities() a\n"
            + "                QUALIFY RowNumber() OVER (\n"
            + "                    PARTITION BY a.Country\n"
            + "                    ORDER BY a.Population DESC, a.City ASC\n"
            + "                ) <= 2\n"
            + "            ), b_ranked AS (\n"
            + "                SELECT b.Country AS Country, b.City AS City,\n"
            + "                       RowNumber() OVER (\n"
            + "                           PARTITION BY b.Country\n"
            + "                           ORDER BY b.Population DESC, b.City ASC\n"
            + "                       ) AS BranchRank\n"
            + "                FROM #B.entities() b\n"
            + "                QUALIFY RowNumber() OVER (\n"
            + "                    PARTITION BY b.Country\n"
            + "                    ORDER BY b.Population DESC, b.City ASC\n"
            + "                ) <= 2\n"
            + "            ), combined AS (\n"
            + "                SELECT Country, City, BranchRank FROM a_ranked\n"
            + "                UNION (Country, City, BranchRank)\n"
            + "                SELECT Country, City, BranchRank FROM b_ranked\n"
            + "            )\n"
            + "            SELECT c.Country AS Country, c.City AS City,\n"
            + "                   c.BranchRank AS BranchRank,\n"
            + "                   RowNumber() OVER (\n"
            + "                       PARTITION BY c.Country\n"
            + "                       ORDER BY c.BranchRank ASC, c.City ASC\n"
            + "                   ) AS CombinedRank\n"
            + "            FROM combined c\n"
            + "            ORDER BY Country, CombinedRank"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Berlin", "DE", 300),
                    new BasicEntity("Hamburg", "DE", 200),
                    new BasicEntity("Munich", "DE", 100),
                    new BasicEntity("Warsaw", "PL", 300),
                    new BasicEntity("Krakow", "PL", 200),
                    new BasicEntity("Gdansk", "PL", 100),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Munich", "DE", 500),
                    new BasicEntity("Berlin", "DE", 400),
                    new BasicEntity("Warsaw", "PL", 400),
                    new BasicEntity("Lodz", "PL", 250),
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("BranchRank", typeof(long)),
            ("CombinedRank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", "Berlin", 1L, 1L],
            ["DE", "Munich", 1L, 2L],
            ["DE", "Berlin", 2L, 3L],
            ["DE", "Hamburg", 2L, 4L],
            ["PL", "Warsaw", 1L, 1L],
            ["PL", "Krakow", 2L, 2L],
            ["PL", "Lodz", 2L, 3L]
        );
    }
}
