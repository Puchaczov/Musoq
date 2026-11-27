using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ComparisonGroupingKeyTests : BasicEntityTestBase
{
    private const string Input = " from values {(x: 1), (x: null), (x: 2), (x: null), (x: 1)} v";

    [TestMethod]
    public void DistinctComparison_ShouldRetainTrueFalseAndUnknown()
    {
        using var query = CreateAndRunVirtualMachine(
            "select distinct x = 1 as Matches" + Input,
            new Dictionary<string, IEnumerable<BasicEntity>>()
        );
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Matches", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [true], [false], [null]);
    }

    [TestMethod]
    public void CompositeDistinctComparison_ShouldRetainTheMatchingTupleForEachValue()
    {
        using var query = CreateAndRunVirtualMachine(
            "select distinct x as Value, x = 1 as Matches" + Input,
            new Dictionary<string, IEnumerable<BasicEntity>>()
        );
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)), ("Matches", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, true], [2, false], [null, null]);
    }

    [TestMethod]
    public void GroupByComparison_ShouldCountUnknownAsItsOwnGroup()
    {
        using var query = CreateAndRunVirtualMachine(
            "select x = 1 as Matches, Count(*) as N" + Input + " group by x = 1",
            new Dictionary<string, IEnumerable<BasicEntity>>()
        );
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Matches", typeof(bool?)), ("N", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [true, 2L], [false, 1L], [null, 2L]);
    }
}
