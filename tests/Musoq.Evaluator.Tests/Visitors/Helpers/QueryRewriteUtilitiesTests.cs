using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}
