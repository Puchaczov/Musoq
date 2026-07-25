using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserContextualKeywordIdentifierTests
{
    [TestMethod]
    public void ReportedExistsColumnQuery_ShouldParseWithoutDiagnostics()
    {
        const string query =
            "select FullPath, Exists from os.files('D:\\repos\\Musoq.Cloud\\src\\dotnet\\Musoq\\bin\\Debug\\net10.0', true) take 5";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        var expression = GetSelectExpression(result.Root!, 1);
        Assert.IsInstanceOfType<IdentifierNode>(expression);
        Assert.AreEqual("Exists", ((IdentifierNode)expression).Name);
    }

    [TestMethod]
    [DataRow("Exists")]
    [DataRow("ANY")]
    [DataRow("Some")]
    [DataRow("all")]
    public void ContextualKeywordColumn_ShouldPreserveIdentifierCase(string identifier)
    {
        var expression = ParseSelectExpression(identifier);

        Assert.IsInstanceOfType<IdentifierNode>(expression);
        Assert.AreEqual(identifier, ((IdentifierNode)expression).Name);
    }

    [TestMethod]
    [DataRow("select Exists from #some.files() where Exists = true")]
    [DataRow("select Exists from #some.files() group by Exists")]
    [DataRow("select Exists from #some.files() order by Exists")]
    [DataRow("select case when Exists then 1 else 0 end from #some.files()")]
    [DataRow("select Coalesce(Exists, false) from #some.files()")]
    [DataRow("select source.Exists from #some.files() source")]
    [DataRow("select source.Exists() from #some.files() source")]
    public void ContextualKeywordColumn_ShouldParseInExpressionContexts(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
    }

    [TestMethod]
    [DataRow("select Exists as Exists from #some.files()")]
    [DataRow("select Any as Some from #some.files()")]
    [DataRow("select Some [All] from #some.files()")]
    [DataRow("select Name as [Select] from #some.files()")]
    public void KeywordColumnsAndAliases_ShouldParseTogether(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
    }

    [TestMethod]
    [DataRow("select Any from #some.files() where Any = true")]
    [DataRow("select Some from #some.files() group by Some")]
    [DataRow("select All from #some.files() order by All")]
    [DataRow("select case when Some then 1 else 0 end from #some.files()")]
    [DataRow("select Coalesce(All, false) from #some.files()")]
    [DataRow("select source.Any from #some.files() source")]
    public void QuantifierKeywordColumn_ShouldParseInExpressionContexts(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
    }

    [TestMethod]
    public void ContextualClauseKeywords_ShouldParseAsColumnsAndPreserveCase()
    {
        foreach (var keyword in KeywordCollisionCatalog.ContextualClauseIdentifiers)
        {
            var identifier = ToMixedCase(keyword);
            var result = ParseWithDiagnostics($"select {identifier} from #some.files()");

            Assert.IsTrue(result.Success,
                $"Contextual identifier '{identifier}' failed:{Environment.NewLine}{result.FormatDiagnostics()}");
            var expression = GetSelectExpression(result.Root!, 0);
            Assert.IsInstanceOfType<IdentifierNode>(expression, $"'{identifier}' was not an identifier.");
            Assert.AreEqual(identifier, ((IdentifierNode)expression).Name);
        }
    }

    [TestMethod]
    public void ContextualClauseKeywords_ShouldParseAsQualifiedProperties()
    {
        foreach (var keyword in KeywordCollisionCatalog.ContextualClauseIdentifiers)
        {
            var result = ParseWithDiagnostics($"select source.{keyword} from #some.files() source");

            Assert.IsTrue(result.Success,
                $"Qualified contextual identifier '{keyword}' failed:{Environment.NewLine}{result.FormatDiagnostics()}");
        }
    }

    [TestMethod]
    public void ReservedSqlKeywords_ShouldNotParseAsUnquotedIdentifiers()
    {
        var failures = new List<string>();

        foreach (var keyword in KeywordCollisionCatalog.ReservedSqlIdentifiers)
        {
            var result = ParseWithDiagnostics($"select {keyword} from #some.files()");

            if (result.Diagnostics.Count != 1)
            {
                failures.Add($"{keyword}: expected one diagnostic, got {result.Diagnostics.Count} ({result.FormatDiagnostics()})");
                continue;
            }

            var expectedCode = keyword is "from" or "distinct"
                ? DiagnosticCode.MQ2005_InvalidSelectList
                : DiagnosticCode.MQ2001_UnexpectedToken;
            if (result.Diagnostics[0].Code != expectedCode)
                failures.Add($"{keyword}: expected {expectedCode}, got {result.Diagnostics[0].Code}");
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void ReservedLiteralKeywords_ShouldRemainLiteralsRatherThanIdentifiers()
    {
        foreach (var keyword in KeywordCollisionCatalog.ReservedLiteralKeywords)
        {
            var result = ParseWithDiagnostics($"select {keyword} from #some.files()");

            Assert.IsTrue(result.Success, result.FormatDiagnostics());
            Assert.IsFalse(GetSelectExpression(result.Root!, 0) is IdentifierNode,
                $"Literal keyword '{keyword}' was incorrectly parsed as an identifier.");
        }
    }

    [TestMethod]
    public void SchemaKeywords_ShouldParseAsColumnsOutsideSchemaContext()
    {
        foreach (var (keyword, _) in KeywordCollisionCatalog.SchemaKeywords)
        {
            var result = ParseWithDiagnostics($"select {keyword} from #some.files()");

            if (keyword is "null")
            {
                Assert.IsTrue(result.Success, result.FormatDiagnostics());
                Assert.IsFalse(GetSelectExpression(result.Root!, 0) is IdentifierNode);
                continue;
            }

            if (keyword is "between")
            {
                Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
                Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, result.Diagnostics[0].Code);
                continue;
            }

            Assert.IsTrue(result.Success,
                $"Schema keyword column '{keyword}' failed:{Environment.NewLine}{result.FormatDiagnostics()}");
            Assert.IsInstanceOfType<IdentifierNode>(GetSelectExpression(result.Root!, 0));
        }
    }

    [TestMethod]
    [DataRow("select Value from #some.files() where Value > any (select Value from #some.rows())")]
    [DataRow("select Value from #some.files() where Value = some (select Value from #some.rows())")]
    [DataRow("select Value from #some.files() where Value < all (select Value from #some.rows())")]
    public void QuantifiedSubqueryOperators_ShouldRemainPredicates(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
    }

    [TestMethod]
    [DataRow("select any(Name, Message) like '%error%' from #some.files()")]
    [DataRow("select all(Name, Message) rlike 'error' from #some.files()")]
    public void PredicateQuantifierCalls_ShouldRemainSupported(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
    }

    [TestMethod]
    public void ExistsWithNonSubqueryArgument_ShouldReportSingleUnexpectedToken()
    {
        var result = ParseWithDiagnostics("select exists(1) from #some.files()");

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, result.Diagnostics[0].Code);
    }

    [TestMethod]
    [DataRow("select exists (select 1 from #some.rows()) from #some.files()")]
    [DataRow("select exists(select 1 from #some.rows()) from #some.files()")]
    public void ExistsFollowedBySubquery_ShouldRemainExistsPredicate(string query)
    {
        var expression = ParseSelectExpression(query, isCompleteQuery: true);

        Assert.IsInstanceOfType<ExistsQueryNode>(expression);
    }

    [TestMethod]
    public void NotExistsFollowedBySubquery_ShouldRemainNegatedExistsPredicate()
    {
        var expression = ParseSelectExpression(
            "select not exists (select 1 from #some.rows()) from #some.files()",
            isCompleteQuery: true);

        Assert.IsInstanceOfType<NotNode>(expression);
    }

    [TestMethod]
    public void BracketQuotedReservedKeywords_ShouldParseAsColumns()
    {
        var keywords = KeywordCollisionCatalog.SqlKeywords
            .Select(keyword => keyword.Text)
            .Concat(KeywordCollisionCatalog.SchemaKeywords.Select(keyword => keyword.Text))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var keyword in keywords)
        {
            var result = ParseWithDiagnostics($"select [{keyword}] from #some.files()");

            Assert.IsTrue(result.Success, $"Keyword '{keyword}' failed:{Environment.NewLine}{result.FormatDiagnostics()}");
            Assert.IsInstanceOfType<IdentifierNode>(GetSelectExpression(result.Root!, 0));
        }
    }

    [TestMethod]
    public void BracketQuotedMultiWordGrammarTokens_ShouldParseAsColumns()
    {
        foreach (var keyword in KeywordCollisionCatalog.MultiWordGrammarTokens)
        {
            var result = ParseWithDiagnostics($"select [{keyword}] from #some.files()");

            Assert.IsTrue(result.Success,
                $"Multi-word keyword '{keyword}' failed:{Environment.NewLine}{result.FormatDiagnostics()}");
            Assert.IsInstanceOfType<IdentifierNode>(GetSelectExpression(result.Root!, 0));
        }
    }

    private static string ToMixedCase(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(chars[i]) : chars[i];

        return new string(chars);
    }

    private static Node ParseSelectExpression(string expression, bool isCompleteQuery = false)
    {
        var query = isCompleteQuery ? expression : $"select {expression} from #some.files()";
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        return GetSelectExpression(root, 0);
    }

    private static Node GetSelectExpression(RootNode root, int fieldIndex)
    {
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query.Select.Fields[fieldIndex].Expression;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new Parser(new Lexer(query, true), diagnostics);
        return parser.ParseWithDiagnostics();
    }
}
