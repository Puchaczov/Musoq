using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class CteColumnListResultTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("", "")]
    [DataRow(" ", " ")]
    [DataRow(" ", "")]
    public void AdjacentCteColumnLists_ShouldPreserveNamesAndAllRows(string firstGap, string secondGap)
    {
        var sql =
            $"with a{firstGap}(X) as (select n from values {{(n:7),(n:2),(n:7)}} v), "
            + $"b{secondGap}(Y) as (select X from a) select Y from b order by Y";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Y", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2], [7], [7]);
    }

    [TestMethod]
    public void RecursiveCteNameBeforeColumnList_ShouldUseTheDeclaredOutput()
    {
        const string sql = """
            with recursive walk(n) as (
                select 1 as start from values {(seed:0)} v
                union all select n + 1 from walk where n < 3
            ) select n from walk order by n
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("n", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2], [3]);
    }
}
