using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

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
        var innerValue = new FieldNode(new IntegerNode("1"), 0, "test");
        var cteInnerExpression = new CteInnerExpressionNode(innerValue, "testCte");


        var outerExpression = new FieldNode(new IntegerNode("2"), 0, "outer");
        _cteNode = new CteExpressionNode([cteInnerExpression], outerExpression);

        _methodNames = new Stack<string>();
        _methodNames.Push("InnerMethod");
        _methodNames.Push("TestMethod");

        _nodes = new Stack<SyntaxNode>();
        _nodes.Push(SyntaxFactory.ExpressionStatement(SyntaxFactory.IdentifierName("testStatement")));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_ReturnsMethodAndName()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        Assert.IsNotNull(result.Method);
        Assert.AreEqual("CteResultQuery", result.MethodName);
        Assert.IsInstanceOfType(result.Method, typeof(MethodDeclarationSyntax));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesCorrectMethodSignature()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        var method = result.Method;
        Assert.AreEqual("CteResultQuery", method.Identifier.Text);
        Assert.AreEqual("Table", method.ReturnType.ToString());
        Assert.AreEqual(1, method.Modifiers.Count);
        Assert.AreEqual(SyntaxKind.PrivateKeyword, method.Modifiers[0].Kind());
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesCorrectParameterList()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


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
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        var method = result.Method;
        var parameters = method.ParameterList.Parameters;

        Assert.AreEqual("ISchemaProvider", parameters[0].Type.ToString());
        Assert.Contains("IReadOnlyDictionary", parameters[1].Type.ToString());
        Assert.Contains("IReadOnlyDictionary", parameters[2].Type.ToString());
        Assert.AreEqual("ILogger", parameters[3].Type.ToString());
        Assert.AreEqual("CancellationToken", parameters[4].Type.ToString());
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CreatesReturnStatement()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        var method = result.Method;
        var statements = method.Body.Statements;


        Assert.AreEqual(2, statements.Count);
        Assert.IsInstanceOfType(statements[1], typeof(ReturnStatementSyntax));

        var returnStatement = (ReturnStatementSyntax)statements[1];
        Assert.IsInstanceOfType(returnStatement.Expression, typeof(InvocationExpressionSyntax));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_CallsCorrectMethodInReturn()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        var method = result.Method;
        var returnStatement = (ReturnStatementSyntax)method.Body.Statements[1];
        var invocation = (InvocationExpressionSyntax)returnStatement.Expression;

        Assert.AreEqual("TestMethod", invocation.Expression.ToString());
        Assert.AreEqual(5, invocation.ArgumentList.Arguments.Count);
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_HasCorrectArgumentsInReturn()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


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
        Assert.Throws<ArgumentNullException>(() =>
            CteExpressionNodeProcessor.ProcessCteExpressionNode(null, _methodNames, _nodes));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_NullMethodNames_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, null, _nodes));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_NullNodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
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

        _methodNames.Push("ExtraMethod");
        _nodes.Push(SyntaxFactory.ExpressionStatement(SyntaxFactory.IdentifierName("testStatement2")));

        // Act
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(multiExpressionNode, _methodNames, _nodes);

        // Assert
        var method = result.Method;
        var statements = method.Body.Statements;


        Assert.AreEqual(3, statements.Count);
        Assert.IsInstanceOfType(statements[0], typeof(ExpressionStatementSyntax));
        Assert.IsInstanceOfType(statements[1], typeof(ExpressionStatementSyntax));
        Assert.IsInstanceOfType(statements[2], typeof(ReturnStatementSyntax));
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_PopsCorrectNumberOfStackItems()
    {
        var initialMethodCount = _methodNames.Count;
        var initialNodeCount = _nodes.Count;


        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        Assert.HasCount(initialMethodCount - 2, _methodNames);

        Assert.HasCount(initialNodeCount - 1, _nodes);
    }

    [TestMethod]
    public void ProcessCteExpressionNode_ValidInput_GeneratesValidCSharpSyntax()
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(_cteNode, _methodNames, _nodes);


        var method = result.Method;
        var syntaxText = method.ToFullString();

        Assert.IsFalse(string.IsNullOrEmpty(syntaxText));
        Assert.Contains("private", syntaxText);
        Assert.Contains("Table", syntaxText);
        Assert.Contains("CteResultQuery", syntaxText);
        Assert.Contains("ISchemaProvider", syntaxText);
        Assert.Contains("return", syntaxText);
    }
}
