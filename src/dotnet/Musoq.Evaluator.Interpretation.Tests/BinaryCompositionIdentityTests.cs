using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryCompositionIdentityTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("Body")]
    [DataRow("Other")]
    public void InlineFields_ShouldUseTheirOwnDeclaringRecord(string secondField)
    {
        var sql = $$"""
            binary One { Body: { A: byte } };
            binary Rec { First: One, {{secondField}}: { B: byte } };
            let b: byte[] = array { 33ub, 42ub };
            select r.First.Body.A, r.{{secondField}}.B from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)33, (byte)42]);
    }

    [TestMethod]
    public void InlineMember_ShouldRemainAvailableInADerivedSchema()
    {
        const string sql = """
            binary One { Body: { A: byte } };
            binary Rec extends One { };
            let b: byte[] = array { 42ub };
            select r.Body.A from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)42]);
    }

    [TestMethod]
    public void InlineFields_ShouldKeepTheirOwnParentBindings()
    {
        const string sql = """
            binary One { N: byte, Body: { X: = parent.N } };
            binary Rec { First: One, M: byte, Body: { Y: = parent.M } };
            let b: byte[] = array { 33ub, 42ub };
            select r.First.Body.X, r.Body.Y from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)33, (byte)42]);
    }

    [TestMethod]
    public void InlineField_ShouldCoexistWithAnExplicitSchemaUsingItsGeneratedName()
    {
        const string sql = """
            binary Inline_Body { X: byte };
            binary Rec { C: Inline_Body, Body: { B: byte } };
            let b: byte[] = array { 33ub, 42ub };
            select r.C.X, r.Body.B from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)33, (byte)42]);
    }

    [TestMethod]
    public void NestedMemberAndUnderscoreMember_ShouldHaveIndependentTypes()
    {
        const string sql = """
            binary Rec { A_B: { P: byte }, A: { B: { Q: byte } } };
            let b: byte[] = array { 33ub, 42ub };
            select r.A_B.P, r.A.B.Q from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)33, (byte)42]);
    }

    [TestMethod]
    public void SwitchFields_ShouldUseTheirOwnDeclaredArms()
    {
        const string sql = """
            binary One { K: byte, Body: switch K { 1 => L: byte } };
            binary Rec { First: One, K: byte, Body: switch K { 2 => R: byte } };
            let b: byte[] = array { 1ub, 33ub, 2ub, 42ub };
            select r.First.Body.L, r.Body.R from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)33, (byte)42]);
    }
}
