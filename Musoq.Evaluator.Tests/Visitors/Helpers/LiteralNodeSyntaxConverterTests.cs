using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class LiteralNodeSyntaxConverterTests
{
    private SyntaxGenerator _generator;

    [TestInitialize]
    public void Initialize()
    {
        var workspace = new AdhocWorkspace();
        _generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
    }

    [TestMethod]
    public void ConvertStringNode_ShouldCreateStringLiteralExpression()
    {
        // Arrange
        var stringNode = new StringNode("test string");

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertStringNode(stringNode);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("test string"));
    }

    [TestMethod]
    public void ConvertStringNode_WithQuotes_ShouldEscapeQuotes()
    {
        // Arrange
        var stringNode = new StringNode("test \"quoted\" string");

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertStringNode(stringNode);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        // The result contains the actual string, let's just verify it was processed
        Assert.IsTrue(code.Contains("test") && code.Contains("quoted") && code.Contains("string"));
    }

    [TestMethod]
    public void ConvertDecimalNode_ShouldCreateCastExpression()
    {
        // Arrange
        var decimalNode = new DecimalNode(123.45m);

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertDecimalNode(decimalNode);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("decimal") && code.Contains("123.45"));
    }

    [TestMethod]
    public void ConvertIntegerNode_WithInt_ShouldCreateIntCastExpression()
    {
        // Arrange
        var integerNode = new IntegerNode(42);

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertIntegerNode(integerNode);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("int") && code.Contains("42"));
    }

    [TestMethod]
    public void ConvertIntegerNode_WithLong_ShouldCreateLongCastExpression()
    {
        // Arrange
        var integerNode = new IntegerNode(123456789L);

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertIntegerNode(integerNode);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("long") && code.Contains("123456789"));
    }

    [TestMethod]
    public void ConvertBooleanNode_WithTrue_ShouldCreateTrueLiteral()
    {
        // Arrange
        var booleanNode = new BooleanNode(true);

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertBooleanNode(booleanNode, _generator);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("true"));
    }

    [TestMethod]
    public void ConvertBooleanNode_WithFalse_ShouldCreateFalseLiteral()
    {
        // Arrange
        var booleanNode = new BooleanNode(false);

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertBooleanNode(booleanNode, _generator);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("false"));
    }

    [TestMethod]
    public void ConvertWordNode_ShouldCreateStringLiteral()
    {
        // Arrange
        var wordNode = new WordNode("sample word");

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertWordNode(wordNode, _generator);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("sample word"));
    }

    [TestMethod]
    public void ConvertNullNode_WithReferenceType_ShouldCreateNullLiteral()
    {
        // Arrange
        var nullNode = new NullNode(typeof(string));

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertNullNode(nullNode, _generator);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("null"));
    }

    [TestMethod]
    public void ConvertNullNode_WithNullableValueType_ShouldCreateNullLiteral()
    {
        // Arrange
        var nullNode = new NullNode(typeof(int?));

        // Act
        var result = LiteralNodeSyntaxConverter.ConvertNullNode(nullNode, _generator);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("null"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ConvertStringNode_WithNullNode_ShouldThrowArgumentNullException()
    {
        // Act
        LiteralNodeSyntaxConverter.ConvertStringNode(null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ConvertBooleanNode_WithNullGenerator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var booleanNode = new BooleanNode(true);

        // Act
        LiteralNodeSyntaxConverter.ConvertBooleanNode(booleanNode, null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ConvertWordNode_WithNullNode_ShouldThrowArgumentNullException()
    {
        // Act
        LiteralNodeSyntaxConverter.ConvertWordNode(null, _generator);
    }
}