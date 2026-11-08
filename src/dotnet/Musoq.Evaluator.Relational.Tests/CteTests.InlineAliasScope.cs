using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    private const string InlineCteAliasReuseQuery =
        "with cte as (select v.Value as Value from values { (Value: 7i) } v) select * from cte v where v.Value = 7i";

    private const string InlineCteChainQuery =
        "with FirstRows as (select v.Value as Projected from values { (Value: 7) } v), SecondRows as (select f.Projected as Result from FirstRows f) select s.Result from SecondRows s";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0129")]
    public void Current_P2_0129_CteChainPreservesOutputName()
    {
        AssertInlineCteIntegerResult(InlineCteChainQuery, "Result");
    }

    [TestMethod]
    public void InlineCteChain_ShouldPreserveExportAliasesAndEstablishedProjectionName()
    {
        AssertInlineCteIntegerResult(InlineCteChainQuery, "Result");
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0058")]
    public void Current_P2_0058_CteAliasCanReuseInnerAlias()
    {
        AssertInlineCteIntegerResult(InlineCteAliasReuseQuery, "Value");
    }

    [TestMethod]
    public void InlineCteAliasReuse_ShouldPreserveSeparateScopesAndEstablishedWildcardName()
    {
        AssertInlineCteIntegerResult(InlineCteAliasReuseQuery, "Value");
    }

    private void AssertInlineCteIntegerResult(string query, string outputName)
    {
        using var compiled = CreateAndRunVirtualMachine(query,
            new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, (outputName, typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7 });
    }
}
