using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0232")]
    public void DerivedUnaliasedProjectionKeepsUnqualifiedName()
    {
        const string query = "select d.Value from (select v.Value from values { (Value: 7) } v) d";
        AssertDerivedProjectionNameAndValue(query, "Value");
    }

    [TestMethod]
    public void DerivedUnaliasedProjectionWithExplicitOuterAlias_ShouldPreserveValue()
    {
        const string query = "select d.Value as Value from (select v.Value from values { (Value: 7) } v) d";
        AssertDerivedProjectionNameAndValue(query, "Value");
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0188")]
    public void DerivedTableExposesProjectionName()
    {
        const string query = "select q.Exposed from (select p.Value as Exposed from values { (Value: 7) } p) q";
        AssertDerivedProjectionNameAndValue(query);
    }

    [TestMethod]
    public void DerivedTableExplicitOuterAlias_ShouldPreserveExportedColumnAndValue()
    {
        const string query = "select q.Exposed as Exposed from (select p.Value as Exposed from values { (Value: 7) } p) q";
        AssertDerivedProjectionNameAndValue(query);
    }

    private void AssertDerivedProjectionNameAndValue(string query, string outputName = "Exposed")
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, (outputName, typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7 });
    }

}
