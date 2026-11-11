using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenPartitionedAsOfLeftJoinFeedsWindowsAndQualify_ShouldPreserveNullExtendedRows()
    {
        const string query = (
            "\n"
            + "            SELECT a.Name AS Event, a.Country AS Country,\n"
            + "                   b.Name AS Match, b.Population AS MatchAt,\n"
            + "                   RowNumber() OVER (\n"
            + "                       PARTITION BY a.Country\n"
            + "                       ORDER BY a.Population ASC, a.Name ASC\n"
            + "                   ) AS Rank,\n"
            + "                   Lag(b.Population) OVER (\n"
            + "                       PARTITION BY a.Country\n"
            + "                       ORDER BY a.Population ASC, a.Name ASC\n"
            + "                   ) AS PreviousMatchAt\n"
            + "            FROM #A.entities() a\n"
            + "            ASOF LEFT JOIN #B.entities() b\n"
            + "                ON a.Country = b.Country AND a.Population >= b.Population\n"
            + "            QUALIFY RowNumber() OVER (\n"
            + "                PARTITION BY a.Country\n"
            + "                ORDER BY a.Population ASC, a.Name ASC\n"
            + "            ) <= 3\n"
            + "            ORDER BY a.Country ASC NULLS LAST, a.Population ASC, a.Name ASC"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        Name = "DE-early",
                        Country = "DE",
                        Population = 50m,
                    },
                    new BasicEntity
                    {
                        Name = "PL-early",
                        Country = "PL",
                        Population = 50m,
                    },
                    new BasicEntity
                    {
                        Name = "PL-mid",
                        Country = "PL",
                        Population = 150m,
                    },
                    new BasicEntity
                    {
                        Name = "PL-late",
                        Country = "PL",
                        Population = 250m,
                    },
                    new BasicEntity
                    {
                        Name = "PL-cut",
                        Country = "PL",
                        Population = 350m,
                    },
                    new BasicEntity
                    {
                        Name = "Null-partition",
                        Country = null,
                        Population = 150m,
                    },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity
                    {
                        Name = "PL-low",
                        Country = "PL",
                        Population = 100m,
                    },
                    new BasicEntity
                    {
                        Name = "PL-high",
                        Country = "PL",
                        Population = 200m,
                    },
                    new BasicEntity
                    {
                        Name = "Null-quote",
                        Country = null,
                        Population = 100m,
                    },
                ]
            },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Event", typeof(string)),
            ("Country", typeof(string)),
            ("Match", typeof(string)),
            ("MatchAt", typeof(decimal?)),
            ("Rank", typeof(long)),
            ("PreviousMatchAt", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE-early", "DE", null, null, 1L, null],
            ["PL-early", "PL", null, null, 1L, null],
            ["PL-mid", "PL", "PL-low", 100m, 2L, null],
            ["PL-late", "PL", "PL-high", 200m, 3L, 100m],
            ["Null-partition", null, null, null, 1L, null]
        );
    }
}
