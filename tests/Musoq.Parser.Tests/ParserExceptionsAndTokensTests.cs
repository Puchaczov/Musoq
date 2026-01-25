using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for Parser exceptions and low-coverage classes (Session 3 - Phase 2)
/// </summary>
[TestClass]
public class ParserExceptionsAndTokensTests
{
    #region Helper Classes

    // Concrete implementation for testing Token base class
    private class TestToken : Token
    {
        public TestToken(string value, TokenType type, TextSpan span)
            : base(value, type, span)
        {
        }
    }

    #endregion

    #region ParserValidationException Tests

    [TestMethod]
    public void ParserValidationException_Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange & Act
        var exception = new ParserValidationException("Test message");

        // Assert
        Assert.AreEqual("Test message", exception.Message);
    }

    [TestMethod]
    public void ParserValidationException_Constructor_WithInnerException_ShouldSetInner()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");

        // Act
        var exception = new ParserValidationException("Outer", inner);

        // Assert
        Assert.AreSame(inner, exception.InnerException);
    }

    [TestMethod]
    public void ParserValidationException_IsArgumentException()
    {
        // Arrange & Act
        var exception = new ParserValidationException("msg");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(ArgumentException));
    }

    [TestMethod]
    public void ParserValidationException_ForNullInput_ShouldCreateProperMessage()
    {
        // Arrange & Act
        var exception = ParserValidationException.ForNullInput();

        // Assert
        Assert.Contains("cannot be null", exception.Message);
    }

    [TestMethod]
    public void ParserValidationException_ForEmptyInput_ShouldCreateProperMessage()
    {
        // Arrange & Act
        var exception = ParserValidationException.ForEmptyInput();

        // Assert
        Assert.Contains("cannot be empty", exception.Message);
        Assert.Contains("whitespace", exception.Message);
    }

    [TestMethod]
    public void ParserValidationException_ForInvalidInput_ShouldIncludeInputAndReason()
    {
        // Arrange
        var input = "SELECT * FORM users";
        var reason = "FORM is not a valid keyword";

        // Act
        var exception = ParserValidationException.ForInvalidInput(input, reason);

        // Assert
        Assert.Contains(input, exception.Message);
        Assert.Contains(reason, exception.Message);
        Assert.Contains("invalid", exception.Message);
    }

    #endregion

    #region SyntaxException Tests

    [TestMethod]
    public void SyntaxException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var message = "Unexpected token";
        var queryPart = "SELECT * FORM";

        // Act
        var exception = new SyntaxException(message, queryPart);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(queryPart, exception.QueryPart);
    }

    [TestMethod]
    public void SyntaxException_WithInnerException_ShouldSetAll()
    {
        // Arrange
        var inner = new InvalidOperationException("inner");

        // Act
        var exception = new SyntaxException("msg", "query", inner);

        // Assert
        Assert.AreEqual("query", exception.QueryPart);
        Assert.AreSame(inner, exception.InnerException);
    }

    [TestMethod]
    public void SyntaxException_IsException()
    {
        // Arrange & Act
        var exception = new SyntaxException("msg", "part");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    #endregion

    #region UnknownTokenException Tests

    [TestMethod]
    public void UnknownTokenException_Constructor_ShouldSetMessage()
    {
        // Arrange & Act
        var exception = new UnknownTokenException(5, '@', "rest of query");

        // Assert
        Assert.Contains("@", exception.Message);
        Assert.Contains("5", exception.Message);
        Assert.Contains("rest of query", exception.Message);
    }

    [TestMethod]
    public void UnknownTokenException_IsException()
    {
        // Arrange & Act
        var exception = new UnknownTokenException(0, 'x', "query");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    [TestMethod]
    public void UnknownTokenException_ShouldMentionUnrecognized()
    {
        // Arrange & Act
        var exception = new UnknownTokenException(10, '#', "remaining");

        // Assert
        Assert.Contains("unrecognized", exception.Message);
    }

    #endregion

    #region TextSpan Tests

    [TestMethod]
    public void TextSpan_Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var span = new TextSpan(10, 5);

        // Assert
        Assert.AreEqual(10, span.Start);
        Assert.AreEqual(5, span.Length);
    }

    [TestMethod]
    public void TextSpan_End_ShouldBeStartPlusLength()
    {
        // Arrange
        var span = new TextSpan(10, 5);

        // Act & Assert
        Assert.AreEqual(15, span.End);
    }

    [TestMethod]
    public void TextSpan_Empty_ShouldHaveZeroValues()
    {
        // Arrange & Act
        var span = TextSpan.Empty;

        // Assert
        Assert.AreEqual(0, span.Start);
        Assert.AreEqual(0, span.Length);
        Assert.AreEqual(0, span.End);
    }

    [TestMethod]
    public void TextSpan_Equals_SameValues_ShouldBeTrue()
    {
        // Arrange
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(5, 10);

        // Act & Assert
        Assert.IsTrue(span1.Equals(span2));
        Assert.IsTrue(span1 == span2);
    }

    [TestMethod]
    public void TextSpan_Equals_DifferentValues_ShouldBeFalse()
    {
        // Arrange
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(5, 11);

        // Act & Assert
        Assert.IsFalse(span1.Equals(span2));
        Assert.IsTrue(span1 != span2);
    }

    [TestMethod]
    public void TextSpan_Equals_Null_ShouldBeFalse()
    {
        // Arrange
        var span = new TextSpan(5, 10);

        // Act & Assert
        Assert.IsFalse(span.Equals(null));
    }

    [TestMethod]
    public void TextSpan_Equals_DifferentType_ShouldBeFalse()
    {
        // Arrange
        var span = new TextSpan(5, 10);

        // Act & Assert
        Assert.IsFalse(span.Equals("not a span"));
    }

    [TestMethod]
    public void TextSpan_GetHashCode_SameValues_ShouldMatch()
    {
        // Arrange
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(5, 10);

        // Act & Assert
        Assert.AreEqual(span1.GetHashCode(), span2.GetHashCode());
    }

    [TestMethod]
    public void TextSpan_GetHashCode_DifferentValues_ShouldDiffer()
    {
        // Arrange
        var span1 = new TextSpan(5, 10);
        var span2 = new TextSpan(6, 10);

        // Act & Assert
        // Note: Hash codes may collide but usually won't for different values
        Assert.AreNotEqual(span1.GetHashCode(), span2.GetHashCode());
    }

    #endregion

    #region Token Tests

    [TestMethod]
    public void Token_ToString_ShouldReturnValue()
    {
        // Arrange
        var token = new TestToken("SELECT", TokenType.Select, TextSpan.Empty);

        // Act
        var result = token.ToString();

        // Assert
        Assert.AreEqual("SELECT", result);
    }

    [TestMethod]
    public void Token_Equals_SameTokenTypeAndValue_ShouldBeTrue()
    {
        // Arrange
        var token1 = new TestToken("test", TokenType.Word, TextSpan.Empty);
        var token2 = new TestToken("test", TokenType.Word, new TextSpan(5, 4));

        // Act
        var result = token1.Equals(token2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Token_Equals_DifferentValue_ShouldBeFalse()
    {
        // Arrange
        var token1 = new TestToken("test1", TokenType.Word, TextSpan.Empty);
        var token2 = new TestToken("test2", TokenType.Word, TextSpan.Empty);

        // Act
        var result = token1.Equals(token2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Token_Equals_DifferentType_ShouldBeFalse()
    {
        // Arrange
        var token1 = new TestToken("SELECT", TokenType.Select, TextSpan.Empty);
        var token2 = new TestToken("SELECT", TokenType.Word, TextSpan.Empty);

        // Act
        var result = token1.Equals(token2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Token_Equals_Null_ShouldBeFalse()
    {
        // Arrange
        var token = new TestToken("test", TokenType.Word, TextSpan.Empty);

        // Act
        var result = token.Equals(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Token_Equals_Object_SameReference_ShouldBeTrue()
    {
        // Arrange
        var token = new TestToken("test", TokenType.Word, TextSpan.Empty);

        // Act
        var result = token.Equals((object)token);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Token_Equals_Object_NotToken_ShouldBeFalse()
    {
        // Arrange
        var token = new TestToken("test", TokenType.Word, TextSpan.Empty);

        // Act
        var result = token.Equals("not a token");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Token_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var token1 = new TestToken("test", TokenType.Word, TextSpan.Empty);
        var token2 = new TestToken("test", TokenType.Word, new TextSpan(10, 4));

        // Act & Assert
        Assert.AreEqual(token1.GetHashCode(), token2.GetHashCode());
    }

    [TestMethod]
    public void Token_Clone_ShouldCreateCopy()
    {
        // Arrange
        var token = new TestToken("SELECT", TokenType.Select, new TextSpan(0, 6));

        // Act
        var clone = token.Clone();

        // Assert
        Assert.AreEqual(token.Value, clone.Value);
        Assert.AreEqual(token.TokenType, clone.TokenType);
    }

    #endregion

    #region PropertyToken Tests

    [TestMethod]
    public void PropertyToken_Constructor_ShouldSetTypeToProperty()
    {
        // Arrange & Act
        var token = new PropertyToken("myProperty", TextSpan.Empty);

        // Assert
        Assert.AreEqual(TokenType.Property, token.TokenType);
        Assert.AreEqual("myProperty", token.Value);
    }

    [TestMethod]
    public void PropertyToken_WithSpan_ShouldSetSpan()
    {
        // Arrange
        var span = new TextSpan(10, 5);

        // Act
        var token = new PropertyToken("prop", span);

        // Assert
        Assert.AreEqual(span, token.Span);
    }

    #endregion

    #region VarArgToken Tests

    [TestMethod]
    public void VarArgToken_Constructor_WithArgsCount_ShouldSetValue()
    {
        // Arrange & Act
        var token = new VarArgToken(3);

        // Assert
        Assert.AreEqual(TokenType.VarArg, token.TokenType);
        Assert.AreEqual("arg", token.Value);
    }

    [TestMethod]
    public void VarArgToken_Constructor_WithName_ShouldSetValue()
    {
        // Arrange & Act
        var token = new VarArgToken("customArg");

        // Assert
        Assert.AreEqual(TokenType.VarArg, token.TokenType);
        Assert.AreEqual("customArg", token.Value);
    }

    [TestMethod]
    public void VarArgToken_TokenText_ShouldBeArg()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("arg", VarArgToken.TokenText);
    }

    #endregion
}
