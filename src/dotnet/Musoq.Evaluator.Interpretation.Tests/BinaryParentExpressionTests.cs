using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class BinaryParentExpressionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("1ub, 65ub, 66ub", "r.C.B.Raw[1]", 66)]
    [DataRow("0ub", "r.C.B.Case", "Empty")]
    public void SwitchArmSize_ShouldReadCompletedEnclosingField(string bytes, string projection, object expected)
    {
        var sql = $$"""
            binary Inner { K: byte, B: switch K { 1 => Raw: byte[parent.N], _ => Empty: byte[0] } };
            binary Rec { N: byte, C: Inner, Tail: byte };
            let b: byte[] = array { 2ub, {{bytes}}, 126ub };
            select {{projection}}, r.Tail from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected is int number ? (byte)number : expected, (byte)126]);
    }

    [TestMethod]
    [DataRow("A[parent.Index]")]
    [DataRow("A[1]")]
    public void LocalArrayIndex_ShouldAcceptAnEnclosingField(string expression)
    {
        var sql = $$"""
            binary Inner { A: byte[2], V: = {{expression}} };
            binary Rec { Index: byte, C: Inner };
            let b: byte[] = array { 1ub, 65ub, 66ub };
            select r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }

    [TestMethod]
    [DataRow("Ns: byte[2]", "parent.Ns[1]")]
    [DataRow("Rows: Item[2]", "parent.Rows[1].N")]
    public void EnclosingArray_ShouldRetainElementTypeWhenIndexed(string declaration, string expression)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Inner { V: = {{expression}} };
            binary Rec { {{declaration}}, C: Inner };
            let b: byte[] = array { 65ub, 66ub };
            select r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }

    [TestMethod]
    [DataRow("parent.R", "A.N")]
    [DataRow("parent.R.N", "A")]
    public void ComputedEnclosingValue_ShouldRetainItsDeclaredType(string alias, string expression)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Inner { A: = {{alias}}, V: = {{expression}} };
            binary Rec { R: Item, C: Inner };
            let b: byte[] = array { 66ub };
            select r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }

    [TestMethod]
    [DataRow("0ub, 0ub", null)]
    [DataRow("1ub, 66ub, 0ub", (byte)66)]
    public void ConditionalEnclosingArray_ShouldPropagateAbsenceThroughAChildIndex(string bytes, byte? expected)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Inner { I: byte, V: = parent.Rows[I].N };
            binary Rec { Flag: byte, Rows: Item[1] when Flag = 1, C: Inner };
            let b: byte[] = array { {{bytes}} };
            select r.C.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }

    [TestMethod]
    public void ReaderWithARenamedParentContext_ShouldForwardItsConstructor()
    {
        const string sql = """
            binary Child { ParentContext: byte, SchemaName: byte, V: = parent.N };
            binary Rec { N: byte, C: Child };
            let b: byte[] = array { 66ub, 16ub, 32ub };
            select r.C.V, r.C.ParentContext, r.C.SchemaName from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66, (byte)16, (byte)32]);
    }

    [TestMethod]
    [DataRow("Parent")]
    [DataRow("parent")]
    public void LocalRecordNamedParent_ShouldTakePrecedenceWithItsDeclaredSpelling(string name)
    {
        var sql = $$"""
            binary Item { N: byte };
            binary Rec { {{name}}: Item, V: = {{name}}.N };
            let b: byte[] = array { 66ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)66]);
    }
}
