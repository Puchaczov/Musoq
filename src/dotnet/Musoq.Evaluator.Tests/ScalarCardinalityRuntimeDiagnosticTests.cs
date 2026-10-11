using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ScalarCardinalityRuntimeDiagnosticTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("select Value from values { (Value: 1), (Value: 2) } s where Value = 9", null)]
    [DataRow("select Value from values { (Value: 1), (Value: 2) } s where Value = 2", 2)]
    [DataRow("select distinct Value from values { (Value: 1), (Value: 1) } s", 1)]
    [DataRow("select Value from values { (Value: 1), (Value: 1) } s group by Value", 1)]
    [DataRow("select Count(Value) from values { (Value: 1), (Value: 2) } s", 2L)]
    [DataRow("select Value from values { (Value: 1), (Value: 2) } s order by Value skip 1 take 1", 2)]
    [DataRow("select Value from values { (Value: 1), (Value: 2) } s take 0", null)]
    public void ScalarCardinality_ShouldCountRowsAfterResultShaping(string scalar, object? expected)
    {
        using var compiled = CompileScalar(scalar);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(expected, result[0].ReadValues()[0]);
    }

    [TestMethod]
    [DataRow("select Value from values { (Value: 1), (Value: 1) } s")]
    [DataRow("select distinct Value from values { (Value: 1), (Value: 2) } s")]
    [DataRow("select Value from values { (Value: 1), (Value: 2) } s group by Value")]
    public void MultipleResultRows_ShouldFailEvenWhenValuesAreEqual(string scalar)
    {
        using var compiled = CompileScalar(scalar);
        AssertCardinality(Assert.Throws<QueryExecutionException>(() =>
            _ = compiled.Run(TestContext.CancellationToken).Count));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DynamicDuplicateRows_ShouldCountRowsInBothCorrelationForms(bool correlated)
    {
        var predicate = correlated ? " where b.Country = a.Country" : string.Empty;
        using var compiled = CreateAndRunVirtualMachine(
            $"select (select b.City from #B.Entities() b{predicate}) as Value from #A.Entities() a",
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity { Country = "PL" }],
                ["#B"] = [new BasicEntity { Country = "PL", City = "same" },
                    new BasicEntity { Country = "PL", City = "same" }]
            });
        AssertCardinality(Assert.Throws<QueryExecutionException>(() =>
            _ = compiled.Run(TestContext.CancellationToken).Count));
    }

    [TestMethod]
    public async Task MultirowScalar_RunAsync_ShouldUseSpecificRuntimeEnvelope()
    {
        using var compiled = CompileScalar("select Value from values { (Value: 1), (Value: 1) } s");
        var failure = await Assert.ThrowsAsync<QueryExecutionException>(async () =>
        {
            using var result = await compiled.RunAsync(TestContext.CancellationToken);
            _ = result.Count;
        });
        AssertCardinality(failure);
    }

    private CompiledQuery CompileScalar(string scalar) => CreateAndRunVirtualMachine(
        $"select ({scalar}) as Value from values {{ (Marker: 0) }} o",
        new Dictionary<string, IEnumerable<BasicEntity>>());

    private static void AssertCardinality(QueryExecutionException failure)
    {
        MusoqExceptionAssertions.AssertRuntimeError(failure,
            DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation);
        Assert.AreEqual("Scalar subquery returned more than one row.", failure.Envelope!.Message);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, failure.Envelope.SourceKind);
        Assert.IsNotNull(failure.InnerException);
    }
}
