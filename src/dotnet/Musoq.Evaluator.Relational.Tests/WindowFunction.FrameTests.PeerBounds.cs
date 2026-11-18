using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    [TestMethod]
    public void SharedPeerFrameSumPreservesCheckedDecimalOverflow()
    {
        const string sql =
            "select Sum(Population) over (order by Id range between 1 preceding and current row) as Total from #A.Entities()";
        var rows = CreateSingleSource(
            new BasicEntity("first") { Id = 1, Population = decimal.MaxValue },
            new BasicEntity("second") { Id = 1, Population = 1m }
        );
        using var query = CreateAndRunVirtualMachine(sql, rows);
        var failure = Assert.Throws<QueryExecutionException>(() => query.Run(TestContext.CancellationToken).ToArray());
        Assert.IsInstanceOfType<OverflowException>(failure.InnerException);
    }

    [TestMethod]
    public void SharedDescendingRangeFramesKeepPeerValuesCountsAndEmptyFrames()
    {
        const string frame =
            "over (partition by Country order by NullableValue desc nulls last range between 1 following and 2 following)";
        var sql =
            $"select Name, Sum(Population) {frame} as Total, Count(Population) {frame} as Present from #A.Entities() order by Name";
        var rows = CreateSingleSource(
            new BasicEntity("a")
            {
                Country = "one",
                NullableValue = 4,
                Population = 1,
            },
            new BasicEntity("b")
            {
                Country = "one",
                NullableValue = 4,
                Population = 2,
            },
            new BasicEntity("c")
            {
                Country = "one",
                NullableValue = 3,
                Population = 4,
            },
            new BasicEntity("d")
            {
                Country = "one",
                NullableValue = 2,
                Population = 8,
            },
            new BasicEntity("e")
            {
                Country = "two",
                NullableValue = 4,
                Population = 16,
            }
        );
        var result = CreateAndRunVirtualMachine(sql, rows).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            ["a", 12m, 2L],
            ["b", 12m, 2L],
            ["c", 8m, 1L],
            ["d", null, 0L],
            ["e", null, 0L]
        );
    }
}
