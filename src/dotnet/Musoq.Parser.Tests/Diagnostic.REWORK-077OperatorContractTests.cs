using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRework077OperatorContractTests
{
    [TestMethod]
    [DataRow("1 + 2", "AddNode")]
    [DataRow("5 - 2", "HyphenNode")]
    [DataRow("2 * 3", "StarNode")]
    [DataRow("8 / 2", "FSlashNode")]
    [DataRow("9 % 4", "ModuloNode")]
    [DataRow("1 = 1", "EqualityNode")]
    [DataRow("1 <> 2", "DiffNode")]
    [DataRow("1 != 2", "DiffNode")]
    [DataRow("2 > 1", "GreaterNode")]
    [DataRow("2 >= 1", "GreaterOrEqualNode")]
    [DataRow("1 < 2", "LessNode")]
    [DataRow("1 <= 2", "LessOrEqualNode")]
    [DataRow("1 & 3", "BitwiseAndNode")]
    [DataRow("1 | 2", "BitwiseOrNode")]
    [DataRow("1 ^ 3", "BitwiseXorNode")]
    [DataRow("1 << 2", "LeftShiftNode")]
    [DataRow("8 >> 2", "RightShiftNode")]
    [DataRow("null ?? 2", "CoalesceNode")]
    [DataRow("'abc' LIKE 'a%'", "LikeNode")]
    [DataRow("'abc' NOT LIKE 'z%'", "NotNode")]
    [DataRow("'abc' RLIKE 'a.*'", "RLikeNode")]
    [DataRow("'abc' NOT RLIKE 'z.*'", "NotNode")]
    [DataRow("1 IN (1, 2)", "InNode")]
    [DataRow("1 CONTAINS (1, 2)", "ContainsNode")]
    [DataRow("1 IS NULL", "IsNullNode")]
    [DataRow("1 IS NOT NULL", "IsNullNode")]
    [DataRow("1 IS DISTINCT FROM 2", "IsDistinctFromNode")]
    [DataRow("2 BETWEEN 1 AND 3", "BetweenNode")]
    [DataRow("NOT false", "NotNode")]
    public void OperatorTruthTable_ShouldBuildTheDocumentedNodeForEachSpelling(
        string expression,
        string expectedNodeType)
    {
        var parsed = ParseSelectExpression(expression);

        Assert.AreEqual(expectedNodeType, parsed.GetType().Name, expression);
    }

    [TestMethod]
    public void OperatorPrecedenceMatrix_ShouldPreserveDocumentedAssociativity()
    {
        var additive = Assert.IsInstanceOfType<AddNode>(ParseSelectExpression("1 + 2 * 3"));
        Assert.IsInstanceOfType<StarNode>(additive.Right);

        var subtraction = Assert.IsInstanceOfType<HyphenNode>(ParseSelectExpression("1 - 2 - 3"));
        Assert.IsInstanceOfType<HyphenNode>(subtraction.Left);

        var shift = Assert.IsInstanceOfType<LeftShiftNode>(ParseSelectExpression("1 << 2 + 1"));
        Assert.IsInstanceOfType<AddNode>(shift.Right);

        var bitwise = Assert.IsInstanceOfType<BitwiseXorNode>(ParseSelectExpression("1 & 2 | 3 ^ 4"));
        var bitwiseOr = Assert.IsInstanceOfType<BitwiseOrNode>(bitwise.Left);
        Assert.IsInstanceOfType<BitwiseAndNode>(bitwiseOr.Left);

        var coalesce = Assert.IsInstanceOfType<CoalesceNode>(ParseSelectExpression("1 ?? 2 ?? 3"));
        Assert.IsInstanceOfType<CoalesceNode>(coalesce.Right);

        var coalesceArithmetic = Assert.IsInstanceOfType<CoalesceNode>(ParseSelectExpression("1 ?? 2 + 3"));
        Assert.IsInstanceOfType<AddNode>(coalesceArithmetic.Right);

        var logical = ParseWhereExpression("1 = 1 or 2 = 2 and not false");
        var or = Assert.IsInstanceOfType<OrNode>(logical);
        var and = Assert.IsInstanceOfType<AndNode>(or.Right);
        Assert.IsInstanceOfType<NotNode>(and.Right);

        var between = Assert.IsInstanceOfType<BetweenNode>(ParseWhereExpression("2 between 1 + 0 and 3 * 1"));
        Assert.IsInstanceOfType<AddNode>(between.Min);
        Assert.IsInstanceOfType<StarNode>(between.Max);
    }

    [TestMethod]
    public void PatternQuantifiers_ShouldLowerOnlyTheDocumentedContextualForms()
    {
        var any = Assert.IsInstanceOfType<OrNode>(ParseWhereExpression("any(Name, City) like 'A%'"));
        Assert.IsInstanceOfType<LikeNode>(any.Left);
        Assert.IsInstanceOfType<LikeNode>(any.Right);

        var all = Assert.IsInstanceOfType<AndNode>(ParseWhereExpression("all(Name, City) not rlike '^A'"));
        Assert.IsInstanceOfType<NotNode>(all.Left);
        Assert.IsInstanceOfType<NotNode>(all.Right);

        var qualified = ParseWhereExpression("source.any(Name) like 'A%'");
        Assert.IsInstanceOfType<LikeNode>(qualified);
        Assert.IsInstanceOfType<AccessMethodNode>(((LikeNode)qualified).Left);
    }

    [TestMethod]
    public void MembershipAndNullPredicateMatrix_ShouldPreserveOperandsAndNegation()
    {
        var notIn = Assert.IsInstanceOfType<NotNode>(ParseWhereExpression("1 not in (1, 2)"));
        var inNode = Assert.IsInstanceOfType<InNode>(notIn.Expression);
        Assert.HasCount(2, Assert.IsInstanceOfType<ArgsListNode>(inNode.Right).Args);

        var collectionIn = Assert.IsInstanceOfType<CollectionInNode>(ParseWhereExpression("1 in $ids"));
        Assert.IsInstanceOfType<ParameterReferenceNode>(collectionIn.Collection);

        var contains = Assert.IsInstanceOfType<ContainsNode>(ParseWhereExpression("1 contains (1, 2)"));
        Assert.HasCount(2, contains.ToCompareExpression.Args);

        var isNotNull = Assert.IsInstanceOfType<IsNullNode>(ParseWhereExpression("1 is not null"));
        Assert.IsTrue(isNotNull.IsNegated);

        var isNotDistinct = Assert.IsInstanceOfType<IsDistinctFromNode>(
            ParseWhereExpression("1 is not distinct from 2"));
        Assert.IsTrue(isNotDistinct.IsNegated);
    }

    [TestMethod]
    public void QuantifiedSubqueryOperators_ShouldRetainTheirDocumentedLowering()
    {
        var any = ParseWhereExpression("1 = ANY (select 1 from #some.a())");
        Assert.IsInstanceOfType<InQueryNode>(any);

        var some = ParseWhereExpression("1 = SOME (select 1 from #some.a())");
        Assert.IsInstanceOfType<InQueryNode>(some);

        var all = ParseWhereExpression("1 > ALL (select 1 from #some.a())");
        Assert.IsInstanceOfType<NotNode>(all);
        Assert.IsInstanceOfType<ExistsQueryNode>(((NotNode)all).Expression);
    }

    [TestMethod]
    public void UnaryAndAdjacentOperatorNearMisses_ShouldRemainValidWhenUnambiguous()
    {
        var subtraction = Assert.IsInstanceOfType<HyphenNode>(ParseSelectExpression("1 - -1"));
        Assert.IsInstanceOfType<IntegerNode>(subtraction.Right);
        Assert.AreEqual(-1, ((IntegerNode)subtraction.Right).ObjValue);

        var parenthesized = ParseSelectExpression("1 + (-2)");
        Assert.IsInstanceOfType<AddNode>(parenthesized);
    }

    [TestMethod]
    [DataRow(
        "select 1 from #some.a() where 1 + * 2",
        DiagnosticCode.MQ2019_InvalidOperator,
        "*",
        1)]
    [DataRow(
        "select 1 from #some.a() where 1 > < 2",
        DiagnosticCode.MQ2019_InvalidOperator,
        "<",
        1)]
    [DataRow(
        "select 1 from #some.a() where 1 + )",
        DiagnosticCode.MQ2020_MissingOperand,
        ")",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 in ()",
        DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed,
        ")",
        1)]
    [DataRow(
        "select 1 from #some.a() where 1 contains ()",
        DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed,
        ")",
        1)]
    [DataRow(
        "select 1 from #some.a() where 1 2",
        DiagnosticCode.MQ2018_MissingOperator,
        "2",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 +",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 ??",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 >",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 like",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where true and",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 between 0 and",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 between 0",
        DiagnosticCode.MQ2002_MissingToken,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 between 0 or 2",
        DiagnosticCode.MQ2002_MissingToken,
        "or",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 between and 3",
        DiagnosticCode.MQ2020_MissingOperand,
        "and",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 is",
        DiagnosticCode.MQ2002_MissingToken,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 is not",
        DiagnosticCode.MQ2002_MissingToken,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 is not distinct from",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 in 1",
        DiagnosticCode.MQ2002_MissingToken,
        "1",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 contains 1",
        DiagnosticCode.MQ2002_MissingToken,
        "1",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 not like",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 not in ()",
        DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed,
        ")",
        1)]
    [DataRow(
        "select 1 from #some.a() where 1 not in 1",
        DiagnosticCode.MQ2002_MissingToken,
        "1",
        0)]
    [DataRow(
        "select 1 from #some.a() where 1 is distinct from",
        DiagnosticCode.MQ2020_MissingOperand,
        "",
        0)]
    public void InvalidOperatorForms_ShouldIdentifyTheRootCauseAndRepairLocation(
        string query,
        DiagnosticCode expectedCode,
        string offendingText,
        int expectedLength)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var offendingStart = string.IsNullOrEmpty(offendingText)
            ? query.Length
            : query.LastIndexOf(offendingText, StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(offendingStart, expectedLength),
            diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(diagnostic.Span.Start, envelope.Offset);
        Assert.AreEqual(diagnostic.Span.Length, envelope.Length);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    [DataRow("select 1 from #some.a() where 1 +", "Operator '+'")]
    [DataRow("select 1 from #some.a() where 1 >", "Operator '>'")]
    [DataRow("select 1 from #some.a() where 1 like", "Operator 'like'")]
    [DataRow("select 1 from #some.a() where true and", "Operator 'and'")]
    [DataRow("select 1 from #some.a() where 1 between 0 and", "Operator 'BETWEEN'")]
    [DataRow("select 1 from #some.a() where 1 is not distinct from", "Operator 'IS NOT DISTINCT FROM'")]
    public void MissingRightOperandDiagnostic_ShouldNameTheOperatorForRepair(
        string query,
        string expectedMessage)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2020_MissingOperand, diagnostic.Code);
        StringAssert.Contains(diagnostic.Message, expectedMessage);
    }

    private static Node ParseWhereExpression(string expression)
    {
        var result = ParseWithDiagnostics($"select 1 from #some.a() where {expression}");
        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Where?.Expression ??
               throw new InvalidOperationException("Expected the parsed query to contain a WHERE clause.");
    }

    private static Node ParseSelectExpression(string expression)
    {
        var result = ParseWithDiagnostics($"select {expression} from #some.a()");
        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = (StatementsArrayNode)result.Root!.Expression;
        var statement = (SingleSetNode)statements.Statements.Single().Node;
        return statement.Query.Select.Fields.Single().Expression;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
