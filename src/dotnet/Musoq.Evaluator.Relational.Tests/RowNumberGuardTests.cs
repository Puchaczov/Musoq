using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class RowNumberGuardTests : GenericEntityTestBase
{
    [TestMethod]
    [DataRow("case when K=1 then RowNumber() else Unavailable end", " order by K", 1)]
    [DataRow("case when RowNumber()=1 then 7 else Unavailable end", " order by K", 7)]
    [DataRow("case when RowNumber()=1 then 7 else Unavailable end", "", 7)]
    public void PositionInCase_ShouldOnlyReadTheSelectedBranch(string expression, string order, int expected)
    {
        var sql = $"select {expression} as S from #schema.first() a{order}";
        using var compiled = CreateAndRunVirtualMachine(sql, new[] { new GuardedRow(1) });
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
    }

    [TestMethod]
    public void QualifiedPosition_ShouldNumberSurvivorsAndOnlyReadSelectedBranches()
    {
        const string sql =
            "select K, case when RowNumber() <= 2 then RowNumber() else Unavailable end as S "
            + "from #schema.first() a qualify RowNumber() over (order by K) > 1 order by K";
        using var compiled = CreateAndRunVirtualMachine(
            sql,
            new[] { new GuardedRow(3), new GuardedRow(1), new GuardedRow(2) }
        );
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [2, 1], [3, 2]);
    }

    public sealed record GuardedRow(int K)
    {
        public int Unavailable => throw new InvalidOperationException("The unselected branch read its source column.");
    }
}
