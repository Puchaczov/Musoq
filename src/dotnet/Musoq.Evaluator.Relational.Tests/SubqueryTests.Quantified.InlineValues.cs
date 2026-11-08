using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    [DataRow("ANY")]
    [DataRow("SOME")]
    [DataRow("ALL")]
    public void WhenQuantifiedSourcesShareAColumnName_ShouldCompareOuterAndInnerValues(string quantifier)
    {
        var query = $"select N from values {{ (N: 1), (N: 3) }} v where N > {quantifier} (select N from values {{ (N: 2) }} t)";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("N", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [3]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-060")]
    public void WhenAnySubqueryUsesEmptyInlineValues_ShouldFilterEveryOuterRow()
    {
        const string query = "select Number from values { (Number: 1), (Number: 2) } v where Number > any (select Threshold from values { (Threshold: 10) } t where Threshold < 10)";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Number", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-016")]
    public void WhenAllSubqueryUsesInlineValues_ShouldFilterUnqualifiedOuterRows()
    {
        const string query = "select Number from values { (Number: 1), (Number: 2), (Number: 3), (Number: 4) } v where Number > all (select Threshold from values { (Threshold: 1), (Threshold: 2) } t)";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Number", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [3], [4]);
    }

    [TestMethod]
    [DataRow("select 1 from values { (N: 1) } v where N > all (select T from values { (T: 0) } t)")]
    [DataRow("select 1 from values { (N: 1) } v where v.N > all (select T from values { (T: 0) } t)")]
    public void WhenAllSubqueryUsesInlineValues_ShouldBindQualifiedAndUnqualifiedOuterColumns(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("1", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
    }
}
