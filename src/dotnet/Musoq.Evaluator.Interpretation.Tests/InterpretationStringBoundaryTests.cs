using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationStringBoundaryTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void SwitchInEmbeddedText_ShouldReadTheSelectedMember(bool partial, bool onlyOneArm)
    {
        var api = partial ? "PartialInterpret" : "Interpret";
        var root = partial ? "r.ParsedFields" : "r";
        var sql = $$"""
            text A { V: rest }; text B { {{(onlyOneArm ? "W" : "V")}}: rest };
            text T { C: switch { pattern 'a' => A, _ => B } };
            binary Rec { S: string[1] ascii as T, Tail: byte };
            select {{root}}.S.C.V as V, {{root}}.Tail as Tail from values { (H:'617e'), (H:'627e'), (H:'627e') } s
            cross apply {{api}}<Rec>(FromHexToBytes(s.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("V", onlyOneArm ? typeof(object) : typeof(string)),
            ("Tail", typeof(byte))
        );
        string? second = onlyOneArm ? null : "b";
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["a", (byte)126],
            [second, (byte)126],
            [second, (byte)126]
        );
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ComputedFieldThroughEmbeddedTextSwitch_ShouldReadTheConcreteArm(bool onlyOneArm)
    {
        var sql = $$"""
            text A { V: rest }; text B { {{(onlyOneArm ? "W" : "V")}}: rest };
            text T { C: switch { pattern 'a' => A, _ => B } };
            binary Rec { S: string[1] ascii as T, X: = S.C.V };
            select r.X as V from values { (H:'61'), (H:'62') } s
            cross apply Interpret<Rec>(FromHexToBytes(s.H)) r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        AssertColumn(table, 0, "V", onlyOneArm ? typeof(object) : typeof(string));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"], [onlyOneArm ? null : "b"]);
    }

    [TestMethod]
    [DataRow("ltrim rtrim")]
    [DataRow("rtrim ltrim")]
    [DataRow("trim")]
    public void BinaryDirectionalTrim_ShouldRemovePaddingAtBothEnds(string modifiers)
    {
        var sql = $$"""
            binary Rec { V: string[3] ascii {{modifiers}} };
            let b: byte[] = array { 32ub, 65ub, 32ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, ["A"]);
    }

    [TestMethod]
    [DataRow("a~", "<~a~a~!", "a~")]
    [DataRow("~!", "<~~!~!!", "~!")]
    [DataRow("aa", "<~aaXaa!", "aaX")]
    public void EscapedBetween_ShouldKeepTheFollowingClosingDelimiterAvailable(
        string close,
        string input,
        string expected
    )
    {
        var sql = $$"""
            text Line { V: between '<' '{{close}}' escaped '~', Tail: rest };
            select p.V, p.Tail from values { (S: '{{input}}') } s cross apply Parse<Line>(s.S) p
            """;

        AssertQueryRows(sql, [expected, "!"]);
    }

    [TestMethod]
    [DataRow("escaped '~' trim", "a>b")]
    [DataRow("escaped '~' trim upper", "A>B")]
    [DataRow("trim upper escaped '~'", "A>B")]
    public void BetweenWithCustomEscape_ShouldDecodeBeforeApplyingCaptureModifiers(string modifiers, string expected)
    {
        var sql = $$"""
            text Rec { V: between '<' '>' {{modifiers}}, Tail: rest };
            select r.V, r.Tail from values { (S: '< a~>b >x') } s cross apply Parse<Rec>(s.S) r
            """;

        AssertQueryRows(sql, [expected, "x"]);
    }

    [TestMethod]
    [DataRow("pattern 'a|b'", "p.V")]
    [DataRow("pattern 'a|(?<G>b)' capture (G)", "p.V.G")]
    public void PatternAlternatives_ShouldMatchOnlyAtTheCurrentCursor(string field, string projection)
    {
        var sql = $$"""
            text Line { V: {{field}} };
            select s.S, {{projection}} from values { (S: 'xb'), (S: 'b') } s outer apply TryParse<Line>(s.S) p
            """;

        AssertQueryRows(sql, ["xb", null], ["b", "b"]);
    }

    [TestMethod]
    public void ComputedAlias_ShouldPreserveTheDecodedTextRecord()
    {
        const string sql = """
            text Line { V: rest };
            binary Rec { Body: string[1] ascii as Line, Copy: = Body };
            let b: byte[] = array { 65ub };
            select r.Copy.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, ["A"]);
    }

    [TestMethod]
    public void CaseBetweenDecodedTextRecords_ShouldPreserveTheirCommonType()
    {
        const string sql = """
            text Line { V: rest };
            binary Rec {
                A: string[1] ascii as Line,
                B: string[1] ascii as Line,
                Copy: = case when A.V = 'B' then A else B end
            };
            let b: byte[] = array { 65ub, 66ub };
            select r.Copy.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, ["B"]);
    }

    [TestMethod]
    [DataRow(@"\(?", "lazy", "", "(")]
    [DataRow(@"\(??", "greedy", "(", "")]
    [DataRow("[(]?", "lazy", "", "(")]
    public void QuantifierAfterALiteralParenthesis_ShouldHonorTheFieldModifier(
        string pattern,
        string modifier,
        string expected,
        string tail
    )
    {
        var sql = $$"""
            text Line { V: pattern '{{pattern}}' {{modifier}}, Tail: rest };
            select p.V, p.Tail from values { (S: '(') } s cross apply Parse<Line>(s.S) p
            """;

        AssertQueryRows(sql, [expected, tail]);
    }

    [TestMethod]
    [DataRow(@"\u0085", "\u0085")]
    [DataRow(@"\u2028", "\u2028")]
    [DataRow(@"\u2029", "\u2029")]
    [DataRow(@"\n", "\n")]
    public void BinaryComputedLiteral_ShouldPreserveUnicodeLineBreaks(string escape, string expected)
    {
        var sql = $$"""
            binary Rec { V: = '{{escape}}' };
            let b: byte[] = array { 0ub };
            select r.V from values { (N: 0) } s cross apply Interpret<Rec>($b) r
            """;

        AssertQueryRows(sql, [expected]);
    }

    [TestMethod]
    public void TextUntil_ShouldConsumeAUnicodeLineSeparator()
    {
        const string sql = """
            text Line { K: until '\u2028', V: rest };
            select p.K, p.V from values { (S: 'A\u2028B') } s cross apply Parse<Line>(s.S) p
            """;

        AssertQueryRows(sql, ["A", "B"]);
    }
}
