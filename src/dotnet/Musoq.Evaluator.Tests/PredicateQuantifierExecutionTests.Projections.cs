using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class PredicateQuantifierExecutionTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0256")]
    public void PatternQuantifiersCompileAndTreatNullOperandsAsNonMatches()
    {
        const string query = "select p.CaseLabel as CaseLabel, any(p.Primary, p.Secondary) like '%error%' as AnyLike, all(p.Primary, p.Secondary) not like '%error%' as AllNotLike from values { (CaseLabel: 'both-null', Primary: null, Secondary: null), (CaseLabel: 'clear-with-null', Primary: 'clear', Secondary: null), (CaseLabel: 'match-with-null', Primary: null, Secondary: 'error in message') } p";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());

        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result,
            ("CaseLabel", typeof(string)), ("AnyLike", typeof(bool)), ("AllNotLike", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            ["both-null", false, true],
            ["clear-with-null", false, true],
            ["match-with-null", true, false]);
    }
}
