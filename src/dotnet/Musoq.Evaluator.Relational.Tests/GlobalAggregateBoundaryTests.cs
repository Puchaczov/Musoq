using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class GlobalAggregateBoundaryTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("x < 0", 0L)]
    [DataRow("x is null", 1L)]
    public void EmptyAndAllNullInputs_ShouldReturnOneTypedAggregateRow(string filter, long expectedRowCount)
    {
        var sql = $$"""
            select Count(*) as RowsTotal, Count(x) as NonNull, Sum(x) as Total, Avg(x) as Mean, Min(x) as Lowest, Max(x) as Highest
            from values {(x: 1), (x: null)} s where {{filter}}
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("RowsTotal", typeof(long)),
            ("NonNull", typeof(long)),
            ("Total", typeof(int?)),
            ("Mean", typeof(decimal?)),
            ("Lowest", typeof(int?)),
            ("Highest", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expectedRowCount, 0L, null, null, null, null]);
    }
}
