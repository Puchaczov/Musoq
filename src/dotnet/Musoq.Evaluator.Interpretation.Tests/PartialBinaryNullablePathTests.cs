using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class PartialBinaryNullablePathTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("false", null)]
    [DataRow("true", 42)]
    public void PartialResult_ShouldPropagateAnAbsentRecordToItsLeaf(string guard, int? value)
    {
        var sql = $$"""
            binary Leaf { V: byte };
            binary Rec { X: Leaf when {{guard}} };
            let b: byte[] = array { 42ub };
            select r.ParsedFields.X.V from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte?)value]);
    }

    [TestMethod]
    public void RenamedCtePayload_ShouldPreserveNestedRecordNullability()
    {
        const string sql = """
            binary Leaf { V: byte };
            binary Rec { K: byte, X: Leaf when K = 1 };
            let b: byte[] = array { 0ub };
            with c (P) as (select r.ParsedFields from values { (N: 0) } s cross apply PartialInterpret<Rec>($b) r)
            select c.P.X.V from c c
            """;

        AssertQueryRows(sql, new object?[] { null });
    }
}
