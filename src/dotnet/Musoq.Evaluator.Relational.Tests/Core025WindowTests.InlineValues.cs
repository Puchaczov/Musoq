using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class Core025WindowTests
{
    [TestMethod]
    [TestProperty("CandidateId", "P1-086")]
    public void BoundedRows_ShouldIncludeInlineNeighbors()
    {
        const string query = "select N, Sum(N) over (order by N rows between 1 preceding and 1 following) as WindowSum from values { (N: 1), (N: 2), (N: 3) } v order by N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("N", typeof(int)),
            ("WindowSum", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 1, 3 },
            new object?[] { 2, 6 },
            new object?[] { 3, 5 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-087")]
    public void RunningRows_ShouldClampInlinePartitionStart()
    {
        const string query = "select N, Sum(N) over (order by N rows between 2 preceding and current row) as RunningSum from values { (N: 1), (N: 2), (N: 3) } v order by N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("N", typeof(int)),
            ("RunningSum", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 1, 1 },
            new object?[] { 2, 3 },
            new object?[] { 3, 6 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-088")]
    public void RangeFrame_ShouldIncludeEveryInlinePeer()
    {
        const string query = "select K, Sum(V) over (order by K range between unbounded preceding and current row) as WindowSum from values { (K: 1, V: 10), (K: 2, V: 20), (K: 2, V: 20), (K: 3, V: 30) } v order by K";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("K", typeof(int)),
            ("WindowSum", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 1, 10 },
            new object?[] { 2, 50 },
            new object?[] { 2, 50 },
            new object?[] { 3, 80 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-089")]
    public void RowsFrame_ShouldAccumulateInlinePeersIndividually()
    {
        const string query = "select K, Sum(V) over (order by K rows between unbounded preceding and current row) as WindowSum from values { (K: 1, V: 10), (K: 2, V: 20), (K: 2, V: 20), (K: 3, V: 30) } v order by K";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("K", typeof(int)),
            ("WindowSum", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 1, 10 },
            new object?[] { 2, 30 },
            new object?[] { 2, 50 },
            new object?[] { 3, 80 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-097")]
    public void NthValue_ShouldBeNullBeforeRequestedInlineRow()
    {
        const string query = "select N, NthValue(N, 2) over (order by N) as SecondN from values { (N: 1), (N: 2), (N: 3) } v order by N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("N", typeof(int)),
            ("SecondN", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 1, null },
            new object?[] { 2, 2 },
            new object?[] { 3, 2 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-098")]
    public void RunningSum_ShouldRestartPerInlinePartition()
    {
        const string query = "select Service, N, Sum(N) over (partition by Service order by N rows between unbounded preceding and current row) as RunningTotal from values { (Service: 'A', N: 1), (Service: 'A', N: 3), (Service: 'B', N: 2), (Service: 'B', N: 4) } v order by Service, N";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result,
            ("Service", typeof(string)),
            ("N", typeof(int)),
            ("RunningTotal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { "A", 1, 1 },
            new object?[] { "A", 3, 4 },
            new object?[] { "B", 2, 2 },
            new object?[] { "B", 4, 6 });
    }
}
