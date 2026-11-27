using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationSchemaFieldTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("binary Prefix { _: byte }; binary Rec extends Prefix { _: byte, Tail: byte }")]
    [DataRow("binary Rec { _: byte, _: byte, Tail: byte }")]
    public void EachAnonymousField_ShouldConsumeItsBytesAcrossInheritance(string schema)
    {
        var sql = $$"""
            {{schema}};
            let b: byte[] = array { 1ub, 2ub, 126ub };
            select r.Tail from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)126]);
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("1")]
    public void ComputedIndex_ShouldSelectTheLastElementOfAFixedArray(string index)
    {
        var sql = $$"""
            binary Rec { A: byte[2], V: = A[{{index}}] };
            let b: byte[] = array { 1ub, 42ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)42]);
    }

    [TestMethod]
    [DataRow(255)]
    [DataRow(1)]
    public void SignedIndex_ShouldSelectTheSameLastElementFromEitherEnd(int firstByte)
    {
        var sql = $$"""
            binary Rec { I: sbyte, A: byte[2], V: = A[I] };
            let b: byte[] = array { {{firstByte}}ub, 1ub, 42ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)42]);
    }

    [TestMethod]
    public void BinaryFieldsDifferingOnlyByCase_ShouldKeepBothValues()
    {
        const string sql = """
            binary Rec { Id: byte, id: byte };
            let b: byte[] = array { 1ub, 2ub };
            select r.Id as UpperId, r.id as LowerId from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)1, (byte)2]);
    }

    [TestMethod]
    public void TextFieldsDifferingOnlyByCase_ShouldKeepBothValues()
    {
        const string sql = """
            text Rec { Id: chars[1], id: chars[1] };
            select r.Id as UpperId, r.id as LowerId from values { (S: 'xy') } s cross apply Parse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, ["x", "y"]);
    }

    [TestMethod]
    public void ShortText_ShouldSerializeOnlyTheCompletedCaseDistinctField()
    {
        const string sql = """
            text Rec { Id: chars[1], id: chars[1] };
            select r.ErrorField, r.BytesConsumed, ToJson(r.ParsedFields)
            from values { (S: 'x') } s cross apply PartialParse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, ["id", 1, """{"Id":"x"}"""]);
    }

    [TestMethod]
    public void InlineFieldsDifferingOnlyByCase_ShouldKeepBothTypes()
    {
        const string sql = """
            binary Rec { Pair: { V: byte, v: string[1] ascii } };
            let b: byte[] = array { 3ub, 65ub };
            select r.Pair.V as U, r.Pair.v as L from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)3, "A"]);
    }

    [TestMethod]
    public void ExpandedBinaryRecord_ShouldKeepCaseDistinctColumns()
    {
        const string sql = """
            binary Pair { V: byte, v: string[1] ascii };
            binary Rec { Pairs: Pair[1] };
            let b: byte[] = array { 3ub, 65ub };
            select e.V as U, e.v as L from values { (N: 0) } s
            cross apply Interpret<Rec>($b) r cross apply r.Pairs e
            """;

        AssertQueryRows(sql, [(byte)3, "A"]);
    }

    [TestMethod]
    public void RepeatCondition_ShouldReadTheEarlierFieldWhenOnlyItsCaseDiffers()
    {
        const string sql = """
            binary Rec { V: byte, v: byte repeat until v[-1] = V, Tail: byte };
            let b: byte[] = array { 2ub, 1ub, 2ub, 126ub };
            select r.v[0], r.Tail from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)1, (byte)126]);
    }

    [TestMethod]
    public void ParentPathsDifferingOnlyByCase_ShouldReadTheCorrespondingEnclosingFields()
    {
        const string sql = """
            binary Child { X: = parent.V, Y: = parent.v };
            binary Rec { V: byte, v: byte, Body: Child };
            let b: byte[] = array { 2ub, 9ub };
            select r.Body.X, r.Body.Y from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [(byte)2, (byte)9]);
    }
}
