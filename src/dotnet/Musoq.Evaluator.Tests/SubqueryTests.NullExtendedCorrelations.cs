using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
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
