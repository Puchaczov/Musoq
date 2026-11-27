using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DistinctComprehensiveTests
{
    [TestMethod]
    [DataRow("distinct ", "", 1L)]
    [DataRow("distinct ", " where s.K=2", 0L)]
    [DataRow("", "", 1L)]
    public void DistinctGlobalCount_PreservesTheSingleAggregateResult(string modifier, string filter, long expected)
    {
        var sql = $"select {modifier}Count(*) as N from values {{ (K:1) }} s{filter}";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }
}
