using System;
using Microsoft.CodeAnalysis;
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
        Assert.Contains("test string", code);
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

        Assert.IsTrue(code.Contains("test") && code.Contains("quoted") && code.Contains("string"));
    }

    [TestMethod]
    public void ConvertDecimalNode_ShouldCreateCastExpression()
    {
        var decimalNode = new DecimalNode(123.45m);


        var result = LiteralNodeSyntaxConverter.ConvertDecimalNode(decimalNode);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("decimal") && code.Contains("123.45"));
    }

    [TestMethod]
    public void ConvertIntegerNode_WithInt_ShouldCreateIntCastExpression()
    {
        var integerNode = new IntegerNode(42);


        var result = LiteralNodeSyntaxConverter.ConvertIntegerNode(integerNode);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("int") && code.Contains("42"));
    }

    [TestMethod]
    public void ConvertIntegerNode_WithLong_ShouldCreateLongCastExpression()
    {
        var integerNode = new IntegerNode(123456789L);


        var result = LiteralNodeSyntaxConverter.ConvertIntegerNode(integerNode);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("long") && code.Contains("123456789"));
    }

    [TestMethod]
    public void ConvertBooleanNode_WithTrue_ShouldCreateTrueLiteral()
    {
        var booleanNode = new BooleanNode(true);


        var result = LiteralNodeSyntaxConverter.ConvertBooleanNode(booleanNode, _generator);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.Contains("true", code);
    }

    [TestMethod]
    public void ConvertBooleanNode_WithFalse_ShouldCreateFalseLiteral()
    {
        var booleanNode = new BooleanNode(false);


        var result = LiteralNodeSyntaxConverter.ConvertBooleanNode(booleanNode, _generator);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.Contains("false", code);
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
        Assert.Contains("sample word", code);
    }

    [TestMethod]
    public void ConvertNullNode_WithReferenceType_ShouldCreateNullLiteral()
    {
        var nullNode = new NullNode(typeof(string));


        var result = LiteralNodeSyntaxConverter.ConvertNullNode(nullNode, _generator);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.Contains("null", code);
    }

    [TestMethod]
    public void ConvertNullNode_WithNullableValueType_ShouldCreateNullLiteral()
    {
        var nullNode = new NullNode(typeof(int?));


        var result = LiteralNodeSyntaxConverter.ConvertNullNode(nullNode, _generator);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.Contains("null", code);
    }

    [TestMethod]
    public void ConvertStringNode_WithNullNode_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LiteralNodeSyntaxConverter.ConvertStringNode(null));
    }

    [TestMethod]
    public void ConvertBooleanNode_WithNullGenerator_ShouldThrowArgumentNullException()
    {
        var booleanNode = new BooleanNode(true);


        Assert.Throws<ArgumentNullException>(() => LiteralNodeSyntaxConverter.ConvertBooleanNode(booleanNode, null));
    }

    [TestMethod]
    public void ConvertWordNode_WithNullNode_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LiteralNodeSyntaxConverter.ConvertWordNode(null, _generator));
    }
}
