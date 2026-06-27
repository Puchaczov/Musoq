using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class PostfixCastParsingTests
{
    [TestMethod]
    public void PostfixCast_OnIdentifier_ShouldCreateCastNode()
    {
        var query = ParseSingleQuery("select Population::Int32 from schema.method()");

        var cast = AssertExpression<CastNode>(query, 0);

        Assert.AreEqual("Int32", cast.TargetTypeName);
        Assert.IsInstanceOfType<IdentifierNode>(cast.Expression);
        Assert.AreEqual("Population::Int32", cast.ToString());
    }

    [TestMethod]
    public void PostfixCast_NestedCasts_ShouldParseLeftToRight()
    {
        var query = ParseSingleQuery("select Name::String::Int32 from schema.method()");

        var outer = AssertExpression<CastNode>(query, 0);
        var inner = Assert.IsInstanceOfType<CastNode>(outer.Expression);

        Assert.AreEqual("Int32", outer.TargetTypeName);
        Assert.AreEqual("String", inner.TargetTypeName);
        Assert.AreEqual("Name::String::Int32", outer.ToString());
    }

    [TestMethod]
    public void PostfixCast_ShouldBindTighterThanAdd()
    {
        var query = ParseSingleQuery("select A + B::Int32 from schema.method()");

        var add = AssertExpression<AddNode>(query, 0);

        Assert.IsInstanceOfType<IdentifierNode>(add.Left);
        Assert.IsInstanceOfType<CastNode>(add.Right);
        Assert.AreEqual("A + B::Int32", add.ToString());
    }

    [TestMethod]
    public void PostfixCast_OnParenthesizedExpression_ShouldCastWholeExpression()
    {
        var query = ParseSingleQuery("select (A + B)::Int32 from schema.method()");

        var cast = AssertExpression<CastNode>(query, 0);

        Assert.IsInstanceOfType<AddNode>(cast.Expression);
        Assert.AreEqual("(A + B)::Int32", cast.ToString());
    }

    [TestMethod]
    [DataRow("select ::1 from schema.method()")]
    [DataRow("select ::Int32 from schema.method()")]
    [DataRow("select Value:: from schema.method()")]
    [DataRow("select Value::1 from schema.method()")]
    public void InvalidPostfixCastForms_ShouldThrowSyntaxException(string query)
    {
        Assert.Throws<SyntaxException>(() => ParseSingleQuery(query));
    }

    private static TNode AssertExpression<TNode>(QueryNode query, int fieldIndex)
        where TNode : Node
    {
        return Assert.IsInstanceOfType<TNode>(query.Select.Fields[fieldIndex].Expression);
    }

    private static QueryNode ParseSingleQuery(string query)
    {
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query;
    }
}
