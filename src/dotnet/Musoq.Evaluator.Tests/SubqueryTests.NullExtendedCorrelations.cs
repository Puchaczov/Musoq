using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedOuterApplyHasNoMatch_ShouldPreserveNullAggregateAndWindow()
    {
        const string query = @"
            SELECT a.Name, a.Country, applied.MatchCount,
                   RowNumber() OVER (ORDER BY a.Name) AS rn
            FROM #A.entities() a
            OUTER APPLY (
                SELECT c.Country, Count(c.City) AS MatchCount
                FROM #C.entities() c
                WHERE c.Country = a.Country
                GROUP BY c.Country
            ) applied
            ORDER BY a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("Alice") { Country = "PL" },
                new BasicEntity("Empty") { Country = "ES" }
            ],
            ["#C"] =
            [
                new BasicEntity("C1") { Country = "PL", City = "KRAKOW" },
                new BasicEntity("C2") { Country = "PL", City = "GDANSK" }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.Country", typeof(string)),
            ("applied.MatchCount", typeof(long?)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", "PL", 2L, 1L],
            ["Empty", "ES", null, 2L]);
    }

    [TestMethod]
    public void WhenFullOuterJoinFeedsCorrelatedSubqueries_ShouldPreserveBothNullExtendedSides()
    {
        const string query = @"
            SELECT a.Name AS LeftName,
                   b.Name AS RightName,
                   EXISTS (
                       SELECT c.Name FROM #C.entities() c
                       WHERE c.Country = a.Country
                   ) AS HasLeftDetail,
                   CASE WHEN a.Country IN (
                       SELECT c.Country FROM #C.entities() c
                       WHERE c.Id = a.Id
                   ) THEN 'Y' ELSE 'N' END AS LeftIn,
                   CASE WHEN b.Country IN (
                       SELECT d.Country FROM #D.entities() d
                       WHERE d.Id = b.Id
                   ) THEN 'Y' ELSE 'N' END AS RightIn,
                   (
                       SELECT Max(d.Population) FROM #D.entities() d
                       WHERE d.Country = b.Country
                   ) AS RightMax
            FROM #A.entities() a
            FULL OUTER JOIN #B.entities() b ON a.Id = b.Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("A-MATCH") { Id = 1, Country = "PL" },
                new BasicEntity("A-LEFT") { Id = 2, Country = "DE" },
                new BasicEntity("A-NULL") { Id = 3, Country = null }
            ],
            ["#B"] =
            [
                new BasicEntity("B-MATCH") { Id = 1, Country = "PL" },
                new BasicEntity("B-RIGHT") { Id = 4, Country = "FR" },
                new BasicEntity("B-NULL") { Id = 5, Country = null }
            ],
            ["#C"] =
            [
                new BasicEntity("C-PL") { Id = 1, Country = "PL", Population = 10m }
            ],
            ["#D"] =
            [
                new BasicEntity("D-PL") { Id = 1, Country = "PL", Population = 10m },
                new BasicEntity("D-FR") { Id = 4, Country = "FR", Population = 20m },
                new BasicEntity("D-NULL") { Id = 5, Country = null, Population = 99m }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("LeftName", typeof(string)),
            ("RightName", typeof(string)),
            ("HasLeftDetail", typeof(bool)),
            ("LeftIn", typeof(string)),
            ("RightIn", typeof(string)),
            ("RightMax", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A-MATCH", "B-MATCH", true, "Y", "Y", 10m],
            ["A-LEFT", null, false, "N", "N", null],
            ["A-NULL", null, false, "N", "N", null],
            [null, "B-RIGHT", false, "N", "Y", 20m],
            [null, "B-NULL", false, "N", "N", null]);
    }

    [TestMethod]
    public void WhenLeftJoinNullableKeyFeedsExistsAndScalarMax_ShouldNotMatchNullToNull()
    {
        const string query = @"
            SELECT a.Name, b.Name, b.Country,
                   EXISTS (
                       SELECT c.Name FROM #C.entities() c
                       WHERE c.Country = b.Country
                   ) as HasRelated,
                   (
                       SELECT Max(c.Population) FROM #C.entities() c
                       WHERE c.Country = b.Country
                   ) as MaxRelated
            FROM #A.entities() a
            LEFT OUTER JOIN #B.entities() b ON a.Id = b.Id
            ORDER BY a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alpha") { Id = 1 },
                    new BasicEntity("Beta") { Id = 2 },
                    new BasicEntity("NullKey") { Id = 3 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("B-PL") { Id = 1, Country = "PL" },
                    new BasicEntity("B-NULL") { Id = 3, Country = null }
                ]
            },
            {
                "#C", [
                    new BasicEntity("C-PL") { Country = "PL", Population = 10m },
                    new BasicEntity("C-NULL") { Country = null, Population = 99m }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)),
            ("b.Country", typeof(string)),
            ("HasRelated", typeof(bool)),
            ("MaxRelated", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alpha", "B-PL", "PL", true, 10m],
            ["Beta", null, null, false, null],
            ["NullKey", "B-NULL", null, false, null]);
    }

    [TestMethod]
    public void WhenRightJoinNullableKeyFeedsExistsAndScalarMax_ShouldNotMatchNullToNull()
    {
        const string query = @"
            SELECT b.Name AS RightName, a.NullableValue AS LeftKey,
                   EXISTS (
                       SELECT c.Name FROM #C.entities() c
                       WHERE c.NullableValue = a.NullableValue
                   ) AS HasDetail,
                   (
                       SELECT Max(d.Population) FROM #D.entities() d
                       WHERE d.NullableValue = a.NullableValue
                   ) AS MaxValue
            FROM #A.entities() a
            RIGHT OUTER JOIN #B.entities() b ON a.Id = b.Id
            ORDER BY b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Left-Key") { Id = 1, NullableValue = 10 },
                    new BasicEntity("Left-Null") { Id = 2, NullableValue = null },
                    new BasicEntity("Left-Only") { Id = 9, NullableValue = 99 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("Right-Key") { Id = 1 },
                    new BasicEntity("Right-Null") { Id = 2 },
                    new BasicEntity("Right-Unmatched") { Id = 3 }
                ]
            },
            {
                "#C", [
                    new BasicEntity("Detail-Key") { NullableValue = 10 },
                    new BasicEntity("Detail-Null") { NullableValue = null }
                ]
            },
            {
                "#D", [
                    new BasicEntity("Value-Low") { NullableValue = 10, Population = 100m },
                    new BasicEntity("Value-High") { NullableValue = 10, Population = 200m },
                    new BasicEntity("Value-Null") { NullableValue = null, Population = 999m }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("RightName", typeof(string)),
            ("LeftKey", typeof(int?)),
            ("HasDetail", typeof(bool)),
            ("MaxValue", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Right-Key", 10, true, 200m],
            ["Right-Null", null, false, null],
            ["Right-Unmatched", null, false, null]);
    }
}
