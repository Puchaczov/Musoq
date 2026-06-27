using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserSetOperatorOptionalKeysTests
{
    [TestMethod]
    [DataRow("union", typeof(UnionNode))]
    [DataRow("union all", typeof(UnionAllNode))]
    [DataRow("except", typeof(ExceptNode))]
    [DataRow("intersect", typeof(IntersectNode))]
    public void Parse_WhenSetOperatorOmitsKeys_ShouldCreateSetOperatorWithEmptyKeys(
        string setOperator,
        Type expectedNodeType)
    {
        var node = ParseSetOperator(
            $"select Col from schemaA.methodA() {setOperator} select Col from schemaB.methodB()");

        Assert.IsInstanceOfType(node, expectedNodeType);
        Assert.IsEmpty(node.Keys);
    }

    [TestMethod]
    [DataRow("union", typeof(UnionNode))]
    [DataRow("union all", typeof(UnionAllNode))]
    [DataRow("except", typeof(ExceptNode))]
    [DataRow("intersect", typeof(IntersectNode))]
    public void Parse_WhenSetOperatorUsesEmptyKeyList_ShouldCreateSetOperatorWithEmptyKeys(
        string setOperator,
        Type expectedNodeType)
    {
        var node = ParseSetOperator(
            $"select Col from schemaA.methodA() {setOperator} () select Col from schemaB.methodB()");

        Assert.IsInstanceOfType(node, expectedNodeType);
        Assert.IsEmpty(node.Keys);
    }

    [TestMethod]
    [DataRow("union", typeof(UnionNode))]
    [DataRow("union all", typeof(UnionAllNode))]
    [DataRow("except", typeof(ExceptNode))]
    [DataRow("intersect", typeof(IntersectNode))]
    public void Parse_WhenSetOperatorUsesExplicitKeys_ShouldPreserveKeys(
        string setOperator,
        Type expectedNodeType)
    {
        var node = ParseSetOperator(
            $"select Col1, Col2 from schemaA.methodA() {setOperator} (Col1, Col2) select Col1, Col2 from schemaB.methodB()");

        Assert.IsInstanceOfType(node, expectedNodeType);
        CollectionAssert.AreEqual(new[] { "Col1", "Col2" }, node.Keys);
    }

    [TestMethod]
    public void Parse_WhenChainedSetOperatorsMixKeyStyles_ShouldPreserveEachOperatorKeys()
    {
        var node = ParseSetOperator(
            """
            select Col from schemaA.methodA()
            union select Col from schemaB.methodB()
            except () select Col from schemaC.methodC()
            intersect (Col) select Col from schemaD.methodD()
            """);

        Assert.IsInstanceOfType<UnionNode>(node);
        Assert.IsEmpty(node.Keys);

        var except = AssertRightSetOperator<ExceptNode>(node);
        Assert.IsEmpty(except.Keys);

        var intersect = AssertRightSetOperator<IntersectNode>(except);
        CollectionAssert.AreEqual(new[] { "Col" }, intersect.Keys);
    }

    [TestMethod]
    public void Parse_WhenCteBodyUsesOmittedSetOperatorKeys_ShouldParse()
    {
        var root = Parse(
            """
            with cte as (
                select Col from schemaA.methodA()
                union
                select Col from schemaB.methodB()
            )
            select Col from cte
            """);

        Assert.IsNotNull(root);
    }

    private static SetOperatorNode ParseSetOperator(string query)
    {
        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;
        return (SetOperatorNode)statements.Statements[0].Node;
    }

    private static T AssertRightSetOperator<T>(SetOperatorNode node)
        where T : SetOperatorNode
    {
        Assert.IsInstanceOfType<T>(node.Right);
        return (T)node.Right;
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }
}
