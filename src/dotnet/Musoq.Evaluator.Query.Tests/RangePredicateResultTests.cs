using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class RangePredicateResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void NotBetween_ShouldMatchNegatedInclusiveRangeAndPreserveUnknown()
    {
        const string sql = """
            select x, x NoT /* bounds */ BeTwEeN 1 and 10 as Outside,
                   not (x between 1 and 10) as Control,
                   x not between null and 10 as MissingLower
            from values {(x: 0), (x: 1), (x: 5), (x: 10), (x: 11), (x: null)} s
            order by x nulls last
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [0, true, true, null],
            [1, false, false, null],
            [5, false, false, null],
            [10, false, false, null],
            [11, true, true, true],
            [null, null, null, null]
        );
    }

    [TestMethod]
    public void NotBetween_FollowedByAnd_ShouldReturnOnlyQualifyingRows()
    {
        const string sql = """
            select x from values {(x: 0), (x: 1), (x: 10), (x: 11), (x: null)} s
            where x not between 1 and 10 and x > 0
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [11]);
    }

    [TestMethod]
    public void Between_ShouldReturnFalseWhenTheKnownBoundExcludesTheValue()
    {
        const string sql =
            "select 11 between null and 10, 5 between 1 and null, 1 between 1 and 10 from values {(Seed: 0)} s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [false, null, true]);
    }
}
