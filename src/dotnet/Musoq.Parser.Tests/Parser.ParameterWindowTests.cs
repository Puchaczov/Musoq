using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParameterWindowTests
{
    [TestMethod]
    [DataRow("param(n: int = 3)", "$n", "plain")]
    [DataRow("param(n: int = 3)", "$n", "qualify")]
    [DataRow("param(n: int = 3)", "$n", "cte")]
    [DataRow("let n: int = 3", "$n", "qualify")]
    [DataRow("let n: int = 3", "$n", "cte")]
    [DataRow("", "3", "qualify")]
    [DataRow("", "3", "cte")]
    public void DiagnosticParsing_SourceInputWithWindow_ShouldAgreeWithStrictParsing(
        string declaration, string argument, string shape)
    {
        var ranking = $"select RowNumber() over (order by r.Value) as Position from system.range(1, {argument}) r";
        var query = shape switch
        {
            "qualify" => $"{ranking} qualify Position <= 2",
            "cte" => $"with ranked as ({ranking}) select Position from ranked where Position <= 2",
            _ => ranking
        };
        var sql = $"{declaration} {query}";
        var strict = new Parser(new Lexer(sql, true)).ComposeAll();
        var lexer = new Lexer(sql, true);
        var diagnostic = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsTrue(diagnostic.Success, diagnostic.FormatDiagnostics());
        Assert.IsNotNull(diagnostic.Root);
        Assert.AreEqual(strict.ToString(), diagnostic.Root.ToString());

        // Keep declarations verbatim, as Cloud's safe renderer does. This isolates
        // window SQL rendering from the separate declaration rendering contract.
        var statements = (StatementsArrayNode)strict.Expression;
        var statement = statements.Statements[^1].Node;
        var renderedStatement = statement is CteExpressionNode cte
            ? $"with {string.Join(", ", cte.InnerExpression.Select(definition => $"{definition.Name} as ({definition.Value.ToString()})"))} {cte.OuterExpression.ToString()}"
            : statement.ToString();
        var rendered = $"{declaration} {renderedStatement}";
        Assert.IsFalse(rendered.Contains("Musoq.Parser.Nodes.", StringComparison.Ordinal), rendered);
        var renderedLexer = new Lexer(rendered, true);
        var reparsed = new Parser(renderedLexer, renderedLexer.Diagnostics).ParseWithDiagnostics();
        Assert.IsTrue(reparsed.Success, reparsed.FormatDiagnostics());
    }

    [TestMethod]
    [DataRow("RowNumber() over ()", "")]
    [DataRow("RowNumber() over (partition by r.Value order by r.Value)", "")]
    [DataRow("Sum(r.Value) over (order by r.Value rows between unbounded preceding and current row)", "")]
    [DataRow("Sum(r.Value) over (order by r.Value range between 1 preceding and 1 following)", "")]
    [DataRow("RowNumber() over w", "window w as (order by r.Value)")]
    public void WindowSqlRendering_ShouldPreserveFunctionSpecificationAndFrame(string expression, string clause)
    {
        var sql = $"select {expression} as Position from system.range(1, 3) r {clause}";
        var root = new Parser(new Lexer(sql, true)).ComposeAll();
        var rendered = root.ToString();
        Assert.IsFalse(rendered.Contains("Musoq.Parser.Nodes.", StringComparison.Ordinal), rendered);
        StringAssert.Contains(rendered, expression);
        if (clause.Length > 0)
            StringAssert.Contains(rendered, clause);

        var lexer = new Lexer(rendered, true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.AreEqual(rendered, result.Root!.ToString());
    }
}
