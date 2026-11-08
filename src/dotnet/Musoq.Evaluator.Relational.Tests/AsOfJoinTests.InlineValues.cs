using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void InlineAsOfLeft_ShouldPreserveUnmatchedRows()
    {
        const string query = "select a.N as LeftN, b.N as RightN from values { (N: 1), (N: 5) } a asof left join values { (N: 4) } b on a.N >= b.N order by a.N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("LeftN", typeof(int)), ("RightN", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, null], [5, 4]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-080")]
    public void InlineAsOf_ShouldChooseNearestBackwardKey()
    {
        const string query = "select a.N as LeftN, b.N as RightN from values { (N: 5) } a asof join values { (N: 1), (N: 4), (N: 7) } b on a.N >= b.N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("LeftN", typeof(int)),
            ("RightN", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 5, 4 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-081")]
    public void InlineStrictAsOf_ShouldExcludeEqualKey()
    {
        const string query = "select b.N as RightN from values { (N: 5) } a asof join values { (N: 4), (N: 5) } b on a.N > b.N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("RightN", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 4 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-082")]
    public void InlineForwardAsOf_ShouldChooseNearestKey()
    {
        const string query = "select b.N as RightN from values { (N: 5) } a asof join values { (N: 3), (N: 7), (N: 9) } b on a.N <= b.N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("RightN", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 7 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-083")]
    public void InlineAsOf_ShouldPartitionEqualityKeys()
    {
        const string query = "select a.Service as Service, b.N as MatchN from values { (Service: 'A', N: 5), (Service: 'B', N: 5) } a asof join values { (Service: 'A', N: 4), (Service: 'A', N: 6), (Service: 'B', N: 3) } b on a.Service = b.Service and a.N >= b.N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("Service", typeof(string)),
            ("MatchN", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "A", 4 },
            new object?[] { "B", 3 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-084")]
    public void InlineAsOfTieBreak_ShouldOrderLabels()
    {
        const string query = "select b.Label as Label from values { (N: 5) } a asof join values { (N: 4, Label: 'alpha'), (N: 4, Label: 'zeta') } b on a.N >= b.N tie break by b.Label desc nulls last";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "zeta" });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-085")]
    public void InlineAsOf_ShouldSkipNullCandidateKeys()
    {
        const string query = "select b.N as MatchN from values { (N: 5) } a asof join values { (N: null), (N: 3) } b on a.N >= b.N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("MatchN", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 3 });
    }
}
