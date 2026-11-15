using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
[TestCategory("QueryInventoryCompilerReproduction")]
public sealed class CaseExpressionRoundTripTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void MultipleWhenBranches_Source_ShouldParse()
    {
        const string query = """
            SELECT CASE WHEN t.IsLiteral THEN t.Token
                        WHEN t.ConstantKnown THEN t.ConstantValue ELSE t.Token END AS Value
            FROM #tokens.rows() t
            """;

        var root = ParseScript(query);

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        Assert.HasCount(1, statements.Statements);
        var statement = Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[0].Node);
        var expression = Assert.IsInstanceOfType<CaseNode>(statement.Query.Select.Fields[0].Expression);
        Assert.HasCount(2, expression.WhenThenPairs);
    }

    [TestMethod]
    [DataRow("t.Token", "t.ConstantValue")]
    [DataRow("t.Coalesce(t.Token, '')", "t.Coalesce(t.ConstantValue, '')")]
    [DataRow("'WHEN THEN ELSE END'", "'second'")]
    [DataRow("case when t.ConstantKnown then t.ConstantValue else t.Token end", "t.ConstantValue")]
    [DataRow("null", "t.ConstantValue")]
    public void MultipleWhenBranches_AstText_ShouldPreserveParseableExpressions(
        string literalValue,
        string constantValue
    )
    {
        var query = $"""
            SELECT CASE WHEN t.IsLiteral THEN {literalValue}
                        WHEN t.ConstantKnown THEN {constantValue} ELSE t.Token END AS Value
            FROM #tokens.rows() t
            """;
        var original = ParseScript(query);
        var serialized = original.ToString();
        TestContext.WriteLine(serialized);

        var roundTripped = ParseScript(serialized);

        Assert.AreEqual(original.Id, roundTripped.Id, "AST text must preserve the complete CASE expression.");
    }

    [TestMethod]
    public void SingleWhenBranch_AstText_ShouldPreserveParseableExpression()
    {
        const string query =
            "SELECT CASE WHEN t.IsLiteral THEN t.Token ELSE t.ConstantValue END AS Value FROM #tokens.rows() t";
        var original = ParseScript(query);

        var roundTripped = ParseScript(original.ToString());

        Assert.AreEqual(original.Id, roundTripped.Id);
    }

    private static RootNode ParseScript(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer).ComposeAll();
        ParserContractAssertions.AssertCompleteScript(root, lexer);
        return root;
    }
}
