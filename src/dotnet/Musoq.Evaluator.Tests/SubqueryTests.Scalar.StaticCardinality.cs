using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    private const string StaticScalarCardinalityQuery =
        "select (select s.Value from values { (Value: 1), (Value: 2) } s) as ScalarValue from values { (Marker: 0) } o";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0106")]
    public void Current_P2_0106_MultirowScalarSubqueryFailsDuringExecution()
    {
        using var compiled = CompileStaticScalarCardinality();
        var failure = Assert.Throws<Musoq.Evaluator.Exceptions.QueryExecutionException>(() =>
            _ = compiled.Run(TestContext.CancellationToken).Count);
        MusoqExceptionAssertions.AssertRuntimeError(failure,
            DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
        Assert.IsNotNull(failure.Envelope);
        Assert.AreEqual("Scalar subquery returned more than one row.", failure.Envelope.Message);
        Assert.IsNotEmpty(failure.Envelope.SuggestedFixes);
    }

    private CompiledQuery CompileStaticScalarCardinality()
    {
        return CreateAndRunVirtualMachine(StaticScalarCardinalityQuery,
            new Dictionary<string, IEnumerable<BasicEntity>>());
    }
}
