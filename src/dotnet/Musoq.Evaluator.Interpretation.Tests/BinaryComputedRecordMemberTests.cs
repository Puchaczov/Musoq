using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryComputedRecordMemberTests : BasicEntityTestBase
{
    [TestMethod]
    public void ComputedAlias_ShouldRetainItsMembersInAnotherExpression()
    {
        const string sql = """
            binary Child { N: byte };
            binary Rec { X: Child, Y: = X, V: = Y.N };
            let b: byte[] = array { 42ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)42]);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void NullableCaseAlias_ShouldLiftItsScalarMember(int tag)
    {
        var sql = $$"""
            binary Child { N: byte };
            binary Rec { Tag: byte, X: Child, Y: = CASE WHEN Tag = 1 THEN X ELSE NULL END, N: = Y.N };
            let b: byte[] = array { {{tag}}ub, 2ub };
            select r.Y.N, r.N from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;
        byte? expected = tag == 1 ? (byte)2 : null;

        AssertQueryRows(sql, [expected, expected]);
    }

    [TestMethod]
    public void DecodedRecordInCte_ShouldBindTheDerivedFieldType()
    {
        const string sql = """
            binary AVal { X: byte };
            binary BVal { Y: byte };
            binary BaseRec { C: AVal };
            binary Rec extends BaseRec { C: BVal };
            let b: byte[] = array { 42ub };
            with a (P) as (select r.C from values { (N: 0) } s cross apply Interpret<Rec>($b) r)
            select z.P.Y from a z
            """;

        AssertQueryRows(sql, [(byte)42]);
    }
}
