using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class CteExpressionNodeProcessorTests
{
    private CteExpressionNode _cteNode;
    private Stack<string> _methodNames;
    private Stack<SyntaxNode> _nodes;

    [TestInitialize]
    public void Initialize()
    {
        // Create test CTE inner expression
        var innerValue = new FieldNode(new IntegerNode("1"), 0, "test");
        var cteInnerExpression = new CteInnerExpressionNode(innerValue, "testCte");
        
        // Create test CTE node with one inner expression
        var outerExpression = new FieldNode(new IntegerNode("2"), 0, "outer");
        _cteNode = new CteExpressionNode([cteInnerExpression], outerExpression);
        
        _methodNames = new Stack<string>();
        _methodNames.Push("InnerMethod"); // Inner expression method name (pushed first)
        _methodNames.Push("TestMethod");  // Result CTE method name (pushed last, popped first)
        
        _nodes = new Stack<SyntaxNode>();
        _nodes.Push(SyntaxFactory.ExpressionStatement(SyntaxFactory.IdentifierName("testStatement")));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_ReturnsMethodAndName()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        Assert.IsNotNull(result.Method);
        Assert.AreEqual("CteResultQuery", result.MethodName);
        Assert.IsInstanceOfType(result.Method, typeof(MethodDeclarationSyntax));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesCorrectMethodSignature()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        Assert.AreEqual("CteResultQuery", method.Identifier.Text);
        Assert.AreEqual("Table", method.ReturnType.ToString());
        Assert.AreEqual(1, method.Modifiers.Count);
        Assert.AreEqual(SyntaxKind.PrivateKeyword, method.Modifiers[0].Kind());
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesCorrectParameterList()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        Assert.AreEqual(5, method.ParameterList.Parameters.Count);
        
        var parameters = method.ParameterList.Parameters;
        Assert.AreEqual("provider", parameters[0].Identifier.Text);
        Assert.AreEqual("positionalEnvironmentVariables", parameters[1].Identifier.Text);
        Assert.AreEqual("queriesInformation", parameters[2].Identifier.Text);
        Assert.AreEqual("logger", parameters[3].Identifier.Text);
        Assert.AreEqual("token", parameters[4].Identifier.Text);
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesCorrectParameterTypes()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var parameters = method.ParameterList.Parameters;
        
        Assert.AreEqual("ISchemaProvider", parameters[0].Type.ToString());
        Assert.IsTrue(parameters[1].Type.ToString().Contains("IReadOnlyDictionary"));
        Assert.IsTrue(parameters[2].Type.ToString().Contains("IReadOnlyDictionary"));
        Assert.AreEqual("ILogger", parameters[3].Type.ToString());
        Assert.AreEqual("CancellationToken", parameters[4].Type.ToString());
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesReturnStatement()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var statements = method.Body.Statements;
        
        // Should have the inner statement plus the return statement
        Assert.AreEqual(2, statements.Count);
        Assert.IsInstanceOfType(statements[1], typeof(ReturnStatementSyntax));
        
        var returnStatement = (ReturnStatementSyntax)statements[1];
        Assert.IsInstanceOfType(returnStatement.Expression, typeof(InvocationExpressionSyntax));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CallsCorrectMethodInReturn()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var returnStatement = (ReturnStatementSyntax)method.Body.Statements[1];
        var invocation = (InvocationExpressionSyntax)returnStatement.Expression;
        
        Assert.AreEqual("TestMethod", invocation.Expression.ToString());
        Assert.AreEqual(5, invocation.ArgumentList.Arguments.Count);
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_HasCorrectArgumentsInReturn()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var returnStatement = (ReturnStatementSyntax)method.Body.Statements[1];
        var invocation = (InvocationExpressionSyntax)returnStatement.Expression;
        var arguments = invocation.ArgumentList.Arguments;
        
        Assert.AreEqual("provider", arguments[0].Expression.ToString());
        Assert.AreEqual("positionalEnvironmentVariables", arguments[1].Expression.ToString());
        Assert.AreEqual("queriesInformation", arguments[2].Expression.ToString());
        Assert.AreEqual("logger", arguments[3].Expression.ToString());
        Assert.AreEqual("token", arguments[4].Expression.ToString());
    }

    [TestMethod]
    public void ProcessCteExpressionNode_NullNode_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            CteExpressionNodeProcessor.ProcessCteExpressionNode(null, _methodNames, _nodes));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_NullMethodNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, null, _nodes));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_NullNodes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, null));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_MultipleInnerExpressions_ProcessesAll()
    {
        // Arrange
        var inner1 = new CteInnerExpressionNode(new FieldNode(new IntegerNode("1"), 0, "test1"), "cte1");
        var inner2 = new CteInnerExpressionNode(new FieldNode(new IntegerNode("2"), 0, "test2"), "cte2");
        var outerExpression = new FieldNode(new IntegerNode("3"), 0, "outer");
        var multiExpressionNode = new CteExpressionNode([inner1, inner2], outerExpression);
        
        _methodNames.Push("ExtraMethod"); // Additional method name for second expression (pushed first)
        _nodes.Push(SyntaxFactory.ExpressionStatement(SyntaxFactory.IdentifierName("testStatement2")));

        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(multiExpressionNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var statements = method.Body.Statements;
        
        // Should have two inner statements plus the return statement
        Assert.AreEqual(3, statements.Count);
        Assert.IsInstanceOfType(statements[0], typeof(ExpressionStatementSyntax));
        Assert.IsInstanceOfType(statements[1], typeof(ExpressionStatementSyntax));
        Assert.IsInstanceOfType(statements[2], typeof(ReturnStatementSyntax));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_PopsCorrectNumberOfStackItems()
    {
        // Arrange
        var initialMethodCount = _methodNames.Count;
        var initialNodeCount = _nodes.Count;

        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        // Should pop one method name for result CTE and one for each inner expression
        Assert.AreEqual(initialMethodCount - 2, _methodNames.Count);
        // Should pop one node for each inner expression
        Assert.AreEqual(initialNodeCount - 1, _nodes.Count);
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_GeneratesValidCSharpSyntax()
    {
        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var syntaxText = method.ToFullString();
        
        // Should be valid C# syntax without syntax errors
        Assert.IsTrue(!string.IsNullOrEmpty(syntaxText));
        Assert.IsTrue(syntaxText.Contains("private"));
        Assert.IsTrue(syntaxText.Contains("Table"));
        Assert.IsTrue(syntaxText.Contains("CteResultQuery"));
        Assert.IsTrue(syntaxText.Contains("ISchemaProvider"));
        Assert.IsTrue(syntaxText.Contains("return"));
    }
}