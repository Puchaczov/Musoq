using System;
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
        var expr1 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1));
        var expr2 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(2));


        var result = SyntaxGenerationHelper.CreateArgumentList(expr1, expr2);


        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Arguments.Count);
        var code = result.ToString();
        Assert.IsTrue(code.Contains("1") && code.Contains("2"));
    }

    [TestMethod]
    public void CreateArgumentList_WithArguments_ShouldCreateCorrectArgumentList()
    {
        var arg1 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(10)));
        var arg2 = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(20)));


        var result = SyntaxGenerationHelper.CreateArgumentList(arg1, arg2);


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
        Assert.Contains("myObject.DoSomething", code);
        Assert.Contains("test", code);
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
        Assert.Contains("calculator.Add", code);
        Assert.Contains("42", code);
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
        Assert.Contains("Helper.Process", code);
        Assert.Contains("input", code);
    }

    [TestMethod]
    public void CreateStringLiteral_ShouldCreateCorrectStringLiteral()
    {
        // Act
        var result = SyntaxGenerationHelper.CreateStringLiteral("hello world");

        // Assert
        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.Contains("hello world", code);
    }

    [TestMethod]
    public void CreateNumericLiteral_ShouldCreateCorrectNumericLiteral()
    {
        var result = SyntaxGenerationHelper.CreateNumericLiteral(123);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("123", code);
    }

    [TestMethod]
    public void CreateBooleanLiteral_WithTrue_ShouldCreateTrueLiteral()
    {
        var result = SyntaxGenerationHelper.CreateBooleanLiteral(true);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("true", code);
    }

    [TestMethod]
    public void CreateBooleanLiteral_WithFalse_ShouldCreateFalseLiteral()
    {
        var result = SyntaxGenerationHelper.CreateBooleanLiteral(false);


        Assert.IsNotNull(result);
        var code = result.ToString();
        Assert.AreEqual("false", code);
    }

    [TestMethod]
    public void CreateNullLiteral_ShouldCreateNullLiteral()
    {
        var result = SyntaxGenerationHelper.CreateNullLiteral();


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
    public void CreateArgumentList_WithNullExpressions_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SyntaxGenerationHelper.CreateArgumentList((ExpressionSyntax[])null));
    }

    [TestMethod]
    public void CreateMethodInvocation_WithNullTarget_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SyntaxGenerationHelper.CreateMethodInvocation((ExpressionSyntax)null, "method"));
    }

    [TestMethod]
    public void CreateStringLiteral_WithNullValue_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SyntaxGenerationHelper.CreateStringLiteral(null));
    }

    [TestMethod]
    public void CreateIdentifier_WithEmptyString_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SyntaxGenerationHelper.CreateIdentifier(""));
    }

    [TestMethod]
    public void CreateMemberAccess_WithEmptyMemberName_ShouldThrowArgumentException()
    {
        // Arrange
        var target = SyntaxFactory.IdentifierName("object");

        // Act
        Assert.Throws<ArgumentException>(() => SyntaxGenerationHelper.CreateMemberAccess(target, ""));
    }
}
