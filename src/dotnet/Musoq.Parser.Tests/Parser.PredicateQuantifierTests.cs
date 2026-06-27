using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserPredicateQuantifierTests
{
    [TestMethod]
    [DataRow("any(Name, Message) like '%error%'", "Name like '%error%' or Message like '%error%'")]
    [DataRow("all(Name, Message) like '%error%'", "Name like '%error%' and Message like '%error%'")]
    [DataRow("any(Name, Message) not like '%error%'", "not (Name like '%error%') or not (Message like '%error%')")]
    [DataRow("all(Name, Message) not like '%error%'", "not (Name like '%error%') and not (Message like '%error%')")]
    [DataRow("any(Name, Message) rlike 'error.*'", "Name rlike 'error.*' or Message rlike 'error.*'")]
    [DataRow("all(Name, Message) rlike 'error.*'", "Name rlike 'error.*' and Message rlike 'error.*'")]
    [DataRow("any(Name, Message) not rlike 'error.*'", "not (Name rlike 'error.*') or not (Message rlike 'error.*')")]
    [DataRow("all(Name, Message) not rlike 'error.*'", "not (Name rlike 'error.*') and not (Message rlike 'error.*')")]
    public void PredicateQuantifier_WithSupportedPatternOperator_ShouldDesugar(string expression, string expected)
    {
        var whereExpression = ParseWhereExpression(expression);

        Assert.AreEqual(expected, whereExpression.ToString());
    }

    [TestMethod]
    public void PredicateQuantifier_WithMixedCaseName_ShouldDesugar()
    {
        var whereExpression = ParseWhereExpression("AnY(Name, Message) like '%error%'");

        Assert.IsInstanceOfType<OrNode>(whereExpression);
        Assert.AreEqual("Name like '%error%' or Message like '%error%'", whereExpression.ToString());
    }

    [TestMethod]
    public void PredicateQuantifier_WithAndComposition_ShouldPreservePrecedence()
    {
        var whereExpression = ParseWhereExpression("any(Name, Message) like '%error%' and Level = 'warn'");

        Assert.IsInstanceOfType<AndNode>(whereExpression);
        var andNode = (AndNode)whereExpression;
        Assert.IsInstanceOfType<OrNode>(andNode.Left);
        Assert.IsInstanceOfType<EqualityNode>(andNode.Right);
    }

    [TestMethod]
    public void PredicateQuantifier_WithOrComposition_ShouldPreservePrecedence()
    {
        var whereExpression = ParseWhereExpression("Level = 'warn' or all(Name, Message) rlike 'error.*'");

        Assert.IsInstanceOfType<OrNode>(whereExpression);
        var orNode = (OrNode)whereExpression;
        Assert.IsInstanceOfType<EqualityNode>(orNode.Left);
        Assert.IsInstanceOfType<AndNode>(orNode.Right);
    }

    [TestMethod]
    public void PredicateQuantifier_WithEmptyAnyArguments_ShouldThrowSyntaxException()
    {
        var exception = Assert.Throws<SyntaxException>(() => ParseWhereExpression("any() like '%error%'"));

        StringAssert.Contains(exception.Message, "ANY requires at least one argument before LIKE.");
    }

    [TestMethod]
    public void PredicateQuantifier_WithEmptyAllArguments_ShouldThrowSyntaxException()
    {
        var exception = Assert.Throws<SyntaxException>(() => ParseWhereExpression("all() rlike 'error.*'"));

        StringAssert.Contains(exception.Message, "ALL requires at least one argument before RLIKE.");
    }

    [TestMethod]
    public void PredicateQuantifier_WithStarArgument_ShouldThrowSyntaxException()
    {
        var exception = Assert.Throws<SyntaxException>(() => ParseWhereExpression("any(*) like '%error%'"));

        StringAssert.Contains(exception.Message, "ANY does not support star arguments; list columns or expressions explicitly.");
    }

    [TestMethod]
    public void PredicateQuantifier_WithAliasedStarArgument_ShouldThrowSyntaxException()
    {
        var exception = Assert.Throws<SyntaxException>(() => ParseWhereExpression("all(source.*) rlike 'error.*'"));

        StringAssert.Contains(exception.Message, "ALL does not support star arguments; list columns or expressions explicitly.");
    }

    [TestMethod]
    public void AnyWithoutPatternOperator_ShouldRemainMethodCall()
    {
        var selectExpression = ParseSingleSelectExpression("any(Name, Message)");

        Assert.IsInstanceOfType<AccessMethodNode>(selectExpression);
        Assert.AreEqual("any", ((AccessMethodNode)selectExpression).Name);
    }

    [TestMethod]
    public void AllWithoutPatternOperator_ShouldRemainMethodCall()
    {
        var selectExpression = ParseSingleSelectExpression("all(Name, Message)");

        Assert.IsInstanceOfType<AccessMethodNode>(selectExpression);
        Assert.AreEqual("all", ((AccessMethodNode)selectExpression).Name);
    }

    [TestMethod]
    public void AliasedAllMethod_WithPatternOperator_ShouldRemainMethodCallPredicate()
    {
        var whereExpression = ParseWhereExpression("dynamic.all(Name, Message) like '%error%'");

        Assert.IsInstanceOfType<LikeNode>(whereExpression);
        var likeNode = (LikeNode)whereExpression;
        Assert.IsInstanceOfType<AccessMethodNode>(likeNode.Left);
        Assert.AreEqual("all", ((AccessMethodNode)likeNode.Left).Name);
    }

    [TestMethod]
    public void UnionAll_ShouldStillParse()
    {
        var query = "select Name from #some.a() union all select Name from #some.b()";

        var result = Parse(query);

        Assert.IsNotNull(result);
    }

    private static Node ParseWhereExpression(string expression)
    {
        var query = $"select 1 from #some.a() where {expression}";
        var result = Parse(query);

        var statements = (StatementsArrayNode)result.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query.Where?.Expression ??
               throw new InvalidOperationException("Expected the parsed query to contain a WHERE clause.");
    }

    private static Node ParseSingleSelectExpression(string expression)
    {
        var query = $"select {expression} from #some.a()";
        var result = Parse(query);

        var statements = (StatementsArrayNode)result.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query.Select.Fields[0].Expression;
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }
}
