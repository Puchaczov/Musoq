using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(80)]
    public void BoundedRowNumberPreservesInputOrderWithMixedOrderingAndNulls(int bound)
    {
        var rows = new[]
        {
            new BasicEntity("a")
            {
                City = "X",
                NullableValue = null,
                Id = 0,
            },
            new BasicEntity("b")
            {
                City = "Y",
                NullableValue = 2,
                Id = 1,
            },
            new BasicEntity("c")
            {
                City = "X",
                NullableValue = 1,
                Id = 2,
            },
            new BasicEntity("d")
            {
                City = "Y",
                NullableValue = null,
                Id = 3,
            },
            new BasicEntity("e")
            {
                City = "X",
                NullableValue = 1,
                Id = 4,
            },
            new BasicEntity("f")
            {
                City = "Y",
                NullableValue = 3,
                Id = 5,
            },
        };
        var expected = rows.GroupBy(row => row.City)
            .SelectMany(group =>
                group
                    .OrderBy(row => row.NullableValue == null)
                    .ThenByDescending(row => row.NullableValue)
                    .ThenByDescending(row => row.Id)
                    .Select((row, index) => (row, rank: (long)index + 1))
                    .Take(bound)
            )
            .OrderBy(pair => pair.row.Id)
            .Select(pair => new object?[] { pair.row.Name, pair.rank })
            .ToArray();
        var sql =
            $"select Name, RowNumber() over (partition by City order by NullableValue desc nulls last, Id desc) as rn from #A.Entities() qualify rn <= {bound}";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource(rows));
        TableMaterializationTestHelper.AssertRowsInOrder(query.Run(TestContext.CancellationToken), expected);
    }

    [TestMethod]
    public void BoundedRowNumberBreaksCompleteTiesWithDistinctOrdinals()
    {
        var rows = Enumerable
            .Range(0, 9)
            .Select(i => new BasicEntity(i.ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                Id = i,
                City = "X",
            })
            .ToArray();
        const string sql =
            "select Id, RowNumber() over (order by City) as rn from #A.Entities() qualify rn <= 2 order by rn";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource(rows));
        var found = query.Run(TestContext.CancellationToken).ToArray();
        CollectionAssert.AreEqual(new long[] { 1, 2 }, found.Select(row => row.GetValue<long>(1)).ToArray());
        Assert.HasCount(2, found.Select(row => row.GetValue<int>(0)).Distinct().ToArray());
        Assert.IsTrue(found.All(row => row.GetValue<int>(0) is >= 0 and < 9));
    }
}
