using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class SelectNodeProcessorTests
{
    [TestMethod]
    public void ProcessSelectNode_ValidInput_GeneratesCorrectBlock()
    {
        // Arrange
        var fieldNode = new FieldNode(new IntegerNode("1", ""), 0, "TestField");
        var selectNode = new SelectNode([fieldNode]);
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        
        var scope = new Scope(null, 0, "test");
        scope["SelectIntoVariableName"] = "resultTable";
        scope["Contexts"] = "test";
        
        // Act
        var result = SelectNodeProcessor.ProcessSelectNode(selectNode, nodes, scope, MethodAccessType.ResultQuery);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Statements.Count);
        
        // Verify variable declaration
        var firstStatement = result.Statements[0] as LocalDeclarationStatementSyntax;
        Assert.IsNotNull(firstStatement);
        Assert.AreEqual("var", firstStatement.Declaration.Type.ToString());
        
        // Verify expression statement
        var secondStatement = result.Statements[1] as ExpressionStatementSyntax;
        Assert.IsNotNull(secondStatement);
    }

    [TestMethod]
    public void ProcessSelectNode_EmptyFields_HandlesEmptyArray()
    {
        // Arrange
        var selectNode = new SelectNode([]);
        var nodes = new Stack<SyntaxNode>();
        
        var scope = new Scope(null, 0, "test");
        scope["SelectIntoVariableName"] = "resultTable";
        scope["Contexts"] = "test";
        
        // Act
        var result = SelectNodeProcessor.ProcessSelectNode(selectNode, nodes, scope, MethodAccessType.ResultQuery);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Statements.Count);
    }

    [TestMethod]
    public void ProcessSelectNode_TransformingQueryType_UsesContextRowVariableName()
    {
        // Arrange
        var fieldNode = new FieldNode(new IntegerNode("1", ""), 0, "TestField");
        var selectNode = new SelectNode([fieldNode]);
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        
        var scope = new Scope(null, 0, "test");
        scope["SelectIntoVariableName"] = "resultTable";
        scope["Contexts"] = "test";
        
        // Act
        var result = SelectNodeProcessor.ProcessSelectNode(selectNode, nodes, scope, MethodAccessType.TransformingQuery);
        
        // Assert
        Assert.IsNotNull(result);
        var generatedCode = result.ToFullString();
        Assert.IsTrue(generatedCode.Contains("testRow"));
    }

    [TestMethod]
    public void ProcessSelectNode_ResultQueryType_UsesScoreVariableName()
    {
        // Arrange
        var fieldNode = new FieldNode(new IntegerNode("1", ""), 0, "TestField");
        var selectNode = new SelectNode([fieldNode]);
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        
        var scope = new Scope(null, 0, "test");
        scope["SelectIntoVariableName"] = "resultTable";
        scope["Contexts"] = "test";
        
        // Act
        var result = SelectNodeProcessor.ProcessSelectNode(selectNode, nodes, scope, MethodAccessType.ResultQuery);
        
        // Assert
        Assert.IsNotNull(result);
        var generatedCode = result.ToFullString();
        Assert.IsTrue(generatedCode.Contains("score"));
    }

    [TestMethod]
    public void ProcessSelectNode_NullSelectNode_ThrowsArgumentNullException()
    {
        // Arrange
        var nodes = new Stack<SyntaxNode>();
        var scope = new Scope(null, 0, "test");
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            SelectNodeProcessor.ProcessSelectNode(null!, nodes, scope, MethodAccessType.ResultQuery));
    }

    [TestMethod]
    public void ProcessSelectNode_NullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldNode = new FieldNode(new IntegerNode("1", ""), 0, "TestField");
        var selectNode = new SelectNode([fieldNode]);
        var scope = new Scope(null, 0, "test");
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            SelectNodeProcessor.ProcessSelectNode(selectNode, null!, scope, MethodAccessType.ResultQuery));
    }

    [TestMethod]
    public void ProcessSelectNode_NullScope_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldNode = new FieldNode(new IntegerNode("1", ""), 0, "TestField");
        var selectNode = new SelectNode([fieldNode]);
        var nodes = new Stack<SyntaxNode>();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            SelectNodeProcessor.ProcessSelectNode(selectNode, nodes, null!, MethodAccessType.ResultQuery));
    }

    [TestMethod]
    public void ProcessSelectNode_GeneratesValidCSharpSyntax()
    {
        // Arrange
        var fieldNode = new FieldNode(new IntegerNode("1", ""), 0, "TestField");
        var selectNode = new SelectNode([fieldNode]);
        var nodes = new Stack<SyntaxNode>();
        nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        
        var scope = new Scope(null, 0, "test");
        scope["SelectIntoVariableName"] = "resultTable";
        scope["Contexts"] = "test";
        
        // Act
        var result = SelectNodeProcessor.ProcessSelectNode(selectNode, nodes, scope, MethodAccessType.ResultQuery);
        
        // Assert
        Assert.IsNotNull(result);
        
        // Verify the generated syntax is valid C#
        var code = result.ToFullString();
        Assert.IsTrue(code.Contains("var select = "));
        Assert.IsTrue(code.Contains("resultTable.Add("));
        Assert.IsTrue(code.Contains("new ObjectsRow("));
        Assert.IsTrue(code.Contains("score.Contexts"));
    }
}