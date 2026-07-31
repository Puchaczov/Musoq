using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class QueryRewriteUtilitiesTests
{
    [TestMethod]
    public void RewriteNullableBoolExpressions_WhenNodeIsNotNullableBool_ShouldReturnOriginalNode()
    {
        var node = new IntegerNode("42");


        var result = QueryRewriteUtilities.RewriteNullableBoolExpressions(node);


        Assert.AreEqual(node, result);
    }

    [TestMethod]
    public void RewriteNullableBoolExpressions_WhenNodeIsBinaryNode_ShouldReturnOriginalNode()
    {
        var binaryNode = new AndNode(new BooleanNode(true), new BooleanNode(false));


        var result = QueryRewriteUtilities.RewriteNullableBoolExpressions(binaryNode);


        Assert.AreEqual(binaryNode, result);
    }

    [TestMethod]
    public void RewriteNullableBoolExpressions_WhenNodeIsNullableBool_ShouldCompareTheNodeWithTrueOnce()
    {
        var node = new TestNode(typeof(bool?), "probe");

        var result = QueryRewriteUtilities.RewriteNullableBoolExpressions(node);

        var equality = Assert.IsInstanceOfType<EqualityNode>(result);
        Assert.AreSame(node, equality.Left);
        Assert.IsInstanceOfType<BooleanNode>(equality.Right);
        Assert.AreEqual("true", equality.Right.ToString(), ignoreCase: true);
        Assert.IsFalse(result is AndNode);
        Assert.IsFalse(result is IsNullNode);
    }

    [TestMethod]
    public void RewriteNullableBoolExpressions_WhenNodeIsNull_ShouldRejectIt()
    {
        Assert.Throws<ArgumentNullException>(() => QueryRewriteUtilities.RewriteNullableBoolExpressions(null!));
    }

    [TestMethod]
    public void RewriteFieldNameWithoutStringPrefixAndSuffix_WhenFieldHasQuotes_ShouldRemoveQuotes()
    {
        // Arrange
        var fieldName = "'test_field'";

        // Act
        var result = QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(fieldName);

        // Assert
        Assert.AreEqual("test_field", result);
    }

    [TestMethod]
    public void RewriteFieldNameWithoutStringPrefixAndSuffix_WhenFieldHasEscapedQuotes_ShouldUnescapeQuotes()
    {
        // Arrange
        var fieldName = @"'test\'s_field'";

        // Act
        var result = QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(fieldName);

        // Assert
        Assert.AreEqual("test's_field", result);
    }

    [TestMethod]
    public void RewriteFieldNameWithoutStringPrefixAndSuffix_WhenNoQuotes_ShouldReturnOriginal()
    {
        // Arrange
        var fieldName = "test_field";

        // Act
        var result = QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(fieldName);

        // Assert
        Assert.AreEqual("test_field", result);
    }

    private sealed class TestNode(Type returnType, string id) : Node
    {
        public override Type ReturnType { get; } = returnType;

        public override string Id { get; } = id;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString() => Id;
    }
}
