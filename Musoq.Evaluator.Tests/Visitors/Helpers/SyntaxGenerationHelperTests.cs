using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class SyntaxGenerationHelperTests
{
    [TestMethod]
    public void CreateArgumentList_WithExpressions_ShouldCreateCorrectArgumentList()
    {
        // Arrange
        var expr1 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1));
        var expr2 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(2));

        // Act
        var result = SyntaxGenerationHelper.CreateArgumentList(expr1, expr2);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Arguments.Count);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("1") && code.Contains("2"));
    }

    [TestMethod]
    public void CreateArgumentList_WithArguments_ShouldCreateCorrectArgumentList()
    {
        // Arrange
        var arg1 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(10)));
        var arg2 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(20)));

        // Act
        var result = SyntaxGenerationHelper.CreateArgumentList(arg1, arg2);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Arguments.Count);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("10") && code.Contains("20"));
    }

    [TestMethod]
    public void CreateMethodInvocation_WithTarget_ShouldCreateCorrectInvocation()
    {
        // Arrange
        var target = SyntaxFactory.IdentifierName("myObject");
        var arg1 = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("test"));

        // Act
        var result = SyntaxGenerationHelper.CreateMethodInvocation(target, "DoSomething", arg1);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("myObject.DoSomething"));
        Assert.IsTrue(code.Contains("test"));
    }

    [TestMethod]
    public void CreateMethodInvocation_WithTargetIdentifier_ShouldCreateCorrectInvocation()
    {
        // Arrange
        var arg1 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(42));

        // Act
        var result = SyntaxGenerationHelper.CreateMethodInvocation("calculator", "Add", arg1);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("calculator.Add"));
        Assert.IsTrue(code.Contains("42"));
    }

    [TestMethod]
    public void CreateStaticMethodInvocation_ShouldCreateCorrectInvocation()
    {
        // Arrange
        var arg1 = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("input"));

        // Act
        var result = SyntaxGenerationHelper.CreateStaticMethodInvocation("Helper", "Process", arg1);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("Helper.Process"));
        Assert.IsTrue(code.Contains("input"));
    }

    [TestMethod]
    public void CreateStringLiteral_ShouldCreateCorrectStringLiteral()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateStringLiteral("hello world");

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("hello world"));
    }

    [TestMethod]
    public void CreateNumericLiteral_ShouldCreateCorrectNumericLiteral()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateNumericLiteral(123);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("123", code);
    }

    [TestMethod]
    public void CreateBooleanLiteral_WithTrue_ShouldCreateTrueLiteral()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateBooleanLiteral(true);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("true", code);
    }

    [TestMethod]
    public void CreateBooleanLiteral_WithFalse_ShouldCreateFalseLiteral()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateBooleanLiteral(false);

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("false", code);
    }

    [TestMethod]
    public void CreateNullLiteral_ShouldCreateNullLiteral()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateNullLiteral();

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("null", code);
    }

    [TestMethod]
    public void CreateIdentifier_ShouldCreateCorrectIdentifier()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateIdentifier("myVariable");

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("myVariable", code);
    }

    [TestMethod]
    public void CreateMemberAccess_WithTarget_ShouldCreateCorrectMemberAccess()
    {
        // Arrange
        var target = SyntaxFactory.IdentifierName("myObject");

        // Act
        var result = SyntaxGenerationHelper.CreateMemberAccess(target, "Property");

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("myObject.Property", code);
    }

    [TestMethod]
    public void CreateMemberAccess_WithTargetIdentifier_ShouldCreateCorrectMemberAccess()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateMemberAccess("instance", "Field");

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("instance.Field", code);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CreateArgumentList_WithNullExpressions_ShouldThrowArgumentNullException()
    {
        // Act
        SyntaxGenerationHelper.CreateArgumentList((ExpressionSyntax[])null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CreateMethodInvocation_WithNullTarget_ShouldThrowArgumentNullException()
    {
        // Act
        SyntaxGenerationHelper.CreateMethodInvocation((ExpressionSyntax)null, "method");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CreateStringLiteral_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act
        SyntaxGenerationHelper.CreateStringLiteral(null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CreateIdentifier_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act
        SyntaxGenerationHelper.CreateIdentifier("");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CreateMemberAccess_WithEmptyMemberName_ShouldThrowArgumentException()
    {
        // Arrange
        var target = SyntaxFactory.IdentifierName("object");

        // Act
        SyntaxGenerationHelper.CreateMemberAccess(target, "");
    }
}