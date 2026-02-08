using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class InterpretCallNodeProcessorTests
{
    private List<string> _addedNamespaces;
    private SyntaxGenerator _generator;
    private Dictionary<string, string> _interpreterInstances;
    private Stack<SyntaxNode> _nodes;
    private List<StatementSyntax> _statements;

    [TestInitialize]
    public void Setup()
    {
        var workspace = new AdhocWorkspace();
        _generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        _nodes = new Stack<SyntaxNode>();
        _statements = [];
        _interpreterInstances = new Dictionary<string, string>();
        _addedNamespaces = [];
    }

    [TestMethod]
    public void ProcessInterpretCallNode_ShouldGenerateInterpreterInvocation()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "BmpHeader");


        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.Contains("Interpret", invocation.ToString());
        Assert.Contains("_interpreter_BmpHeader", invocation.ToString());
        Assert.HasCount(1, _statements);
        Assert.IsTrue(_interpreterInstances.ContainsKey("BmpHeader"));
        Assert.Contains("Musoq.Generated.Interpreters", _addedNamespaces);
    }

    [TestMethod]
    public void ProcessInterpretCallNode_ShouldReuseExistingInterpreterInstance()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "BmpHeader");


        _interpreterInstances["BmpHeader"] = "_interpreter_BmpHeader";


        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.IsEmpty(_statements);
    }

    [TestMethod]
    public void ProcessParseCallNode_ShouldGenerateParseInvocation()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("line", typeof(string));
        var parseNode = new ParseCallNode(dataSourceNode, "CsvRow");


        _nodes.Push(SyntaxFactory.IdentifierName("line"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessParseCallNode(
            parseNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.Contains("Parse", invocation.ToString());
        Assert.Contains("_interpreter_CsvRow", invocation.ToString());
    }

    [TestMethod]
    public void ProcessInterpretAtCallNode_ShouldGenerateInterpretAtInvocation()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("buffer", typeof(byte[]));
        var offsetNode = new IntegerNode("100");
        var interpretAtNode = new InterpretAtCallNode(dataSourceNode, offsetNode, "PacketHeader");


        _nodes.Push(SyntaxFactory.IdentifierName("buffer"));
        _nodes.Push(SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(100)));

        // Act
        var result = InterpretCallNodeProcessor.ProcessInterpretAtCallNode(
            interpretAtNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.Contains("InterpretAt", invocation.ToString());
        Assert.Contains("_interpreter_PacketHeader", invocation.ToString());
        Assert.Contains("100", invocation.ToString());
    }

    [TestMethod]
    public void ProcessInterpretCallNode_WithNullNode_ShouldThrow()
    {
        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        Assert.Throws<ArgumentNullException>(() =>
            InterpretCallNodeProcessor.ProcessInterpretCallNode(
                null,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns)));
    }

    [TestMethod]
    public void ProcessInterpretCallNode_MultipleSchemas_ShouldCreateSeparateInstances()
    {
        // Arrange
        var dataSourceNode1 = new IdentifierNode("data1", typeof(byte[]));
        var interpretNode1 = new InterpretCallNode(dataSourceNode1, "BmpHeader");

        var dataSourceNode2 = new IdentifierNode("data2", typeof(byte[]));
        var interpretNode2 = new InterpretCallNode(dataSourceNode2, "WavHeader");


        _nodes.Push(SyntaxFactory.IdentifierName("data1"));
        InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode1,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));


        _nodes.Push(SyntaxFactory.IdentifierName("data2"));
        InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode2,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        Assert.HasCount(2, _interpreterInstances);
        Assert.IsTrue(_interpreterInstances.ContainsKey("BmpHeader"));
        Assert.IsTrue(_interpreterInstances.ContainsKey("WavHeader"));
        Assert.HasCount(2, _statements);
    }

    [TestMethod]
    public void ProcessInterpretCallNode_GeneratedStatement_ShouldContainNewExpression()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "TestSchema");

        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        Assert.HasCount(1, _statements);
        var statementText = _statements[0].ToString();
        Assert.Contains("_interpreter_TestSchema", statementText);
        // The class name is now "TestSchema" (the schema name), not "TestSchemaInterpreter"
        Assert.Contains("newTestSchema()", statementText);
    }

    [TestMethod]
    public void ProcessInterpretCallNode_WithRegistry_SchemaNotFound_ShouldThrow()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "NonExistentSchema");
        var schemaRegistry = new SchemaRegistry();

        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        var ex = Assert.Throws<UnknownInterpretationSchemaException>(() =>
            InterpretCallNodeProcessor.ProcessInterpretCallNode(
                interpretNode,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns),
                schemaRegistry));

        Assert.AreEqual("NonExistentSchema", ex.SchemaName);
        Assert.Contains("NonExistentSchema", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [TestMethod]
    public void ProcessParseCallNode_WithRegistry_SchemaNotFound_ShouldThrow()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(string));
        var parseNode = new ParseCallNode(dataSourceNode, "NonExistentSchema");
        var schemaRegistry = new SchemaRegistry();

        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        var ex = Assert.Throws<UnknownInterpretationSchemaException>(() =>
            InterpretCallNodeProcessor.ProcessParseCallNode(
                parseNode,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns),
                schemaRegistry));

        Assert.AreEqual("NonExistentSchema", ex.SchemaName);
    }

    [TestMethod]
    public void ProcessInterpretAtCallNode_WithRegistry_SchemaNotFound_ShouldThrow()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("buffer", typeof(byte[]));
        var offsetNode = new IntegerNode("100");
        var interpretAtNode = new InterpretAtCallNode(dataSourceNode, offsetNode, "NonExistentSchema");
        var schemaRegistry = new SchemaRegistry();

        _nodes.Push(SyntaxFactory.IdentifierName("buffer"));
        _nodes.Push(SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(100)));


        var ex = Assert.Throws<UnknownInterpretationSchemaException>(() =>
            InterpretCallNodeProcessor.ProcessInterpretAtCallNode(
                interpretAtNode,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns),
                schemaRegistry));

        Assert.AreEqual("NonExistentSchema", ex.SchemaName);
    }

    [TestMethod]
    public void ProcessInterpretCallNode_WithNullRegistry_ShouldUseFallbackNaming()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "TestSchema");

        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        var result = InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));


        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.Contains("_interpreter_TestSchema", invocation.ToString());
        Assert.Contains("Musoq.Generated.Interpreters", _addedNamespaces);
    }

    [TestMethod]
    public void ProcessInterpretCallNode_WithRegistry_SchemaFoundWithValidType_ShouldWork()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "TestSchema");
        var schemaRegistry = new SchemaRegistry();


        var placeholderNode = new IdentifierNode("placeholder", typeof(object));
        schemaRegistry.Register("TestSchema", placeholderNode);
        schemaRegistry.TryGetSchema("TestSchema", out var registration);
        registration!.GeneratedType = typeof(string);

        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns),
            schemaRegistry);


        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.Contains("_interpreter_TestSchema", invocation.ToString());
        Assert.Contains("System", _addedNamespaces);
    }

    [TestMethod]
    public void ProcessInterpretCallNode_WithRegistry_SchemaFoundButTypeNull_ShouldThrow()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var interpretNode = new InterpretCallNode(dataSourceNode, "IncompleteSchema");
        var schemaRegistry = new SchemaRegistry();


        var placeholderNode = new IdentifierNode("placeholder", typeof(object));
        schemaRegistry.Register("IncompleteSchema", placeholderNode);


        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        var ex = Assert.Throws<UnknownInterpretationSchemaException>(() =>
            InterpretCallNodeProcessor.ProcessInterpretCallNode(
                interpretNode,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns),
                schemaRegistry));

        Assert.AreEqual("IncompleteSchema", ex.SchemaName);
        Assert.IsTrue(ex.Message.Contains("type") || ex.Message.Contains("unavailable"));
    }

    [TestMethod]
    public void ProcessTryInterpretCallNode_ShouldGenerateTryCatchInvocation()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var tryInterpretNode = new TryInterpretCallNode(dataSourceNode, "BmpHeader", typeof(object));


        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessTryInterpretCallNode(
            tryInterpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));


        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        var invocationText = invocation.ToString();

        Assert.Contains("Func<", invocationText);
        Assert.Contains("try", invocationText);
        Assert.Contains("catch", invocationText);
        Assert.HasCount(1, _statements);
        Assert.IsTrue(_interpreterInstances.ContainsKey("BmpHeader"));
    }

    [TestMethod]
    public void ProcessTryInterpretCallNode_ShouldReuseExistingInterpreterInstance()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var tryInterpretNode = new TryInterpretCallNode(dataSourceNode, "BmpHeader", typeof(object));


        _interpreterInstances["BmpHeader"] = "_interpreter_BmpHeader";


        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessTryInterpretCallNode(
            tryInterpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.IsEmpty(_statements);
    }

    [TestMethod]
    public void ProcessTryParseCallNode_ShouldGenerateTryCatchInvocation()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("line", typeof(string));
        var tryParseNode = new TryParseCallNode(dataSourceNode, "CsvRow", typeof(object));


        _nodes.Push(SyntaxFactory.IdentifierName("line"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessTryParseCallNode(
            tryParseNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));


        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        var invocationText = invocation.ToString();

        Assert.Contains("Func<", invocationText);
        Assert.Contains("try", invocationText);
        Assert.Contains("catch", invocationText);
        Assert.IsTrue(_interpreterInstances.ContainsKey("CsvRow"));
    }

    [TestMethod]
    public void ProcessTryInterpretCallNode_WithNullReturnType_ShouldUseObjectNullable()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var tryInterpretNode = new TryInterpretCallNode(dataSourceNode, "TestSchema");

        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessTryInterpretCallNode(
            tryInterpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));


        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        var invocationText = invocation.ToString();
        Assert.Contains("Func<", invocationText);
        Assert.Contains("object?", invocationText);
    }

    [TestMethod]
    public void ProcessTryInterpretCallNode_WithNullNode_ShouldThrow()
    {
        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        Assert.Throws<ArgumentNullException>(() =>
            InterpretCallNodeProcessor.ProcessTryInterpretCallNode(
                null,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns)));
    }

    [TestMethod]
    public void ProcessTryParseCallNode_WithNullNode_ShouldThrow()
    {
        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        Assert.Throws<ArgumentNullException>(() =>
            InterpretCallNodeProcessor.ProcessTryParseCallNode(
                null,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns)));
    }

    [TestMethod]
    public void ProcessPartialInterpretCallNode_ShouldGeneratePartialInterpretInvocation()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var partialInterpretNode = new PartialInterpretCallNode(dataSourceNode, "BmpHeader");


        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessPartialInterpretCallNode(
            partialInterpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        var invocationText = invocation.ToString();
        Assert.Contains("PartialInterpret", invocationText);
        Assert.Contains("_interpreter_BmpHeader", invocationText);
        Assert.HasCount(1, _statements);
        Assert.IsTrue(_interpreterInstances.ContainsKey("BmpHeader"));
        Assert.Contains("Musoq.Generated.Interpreters", _addedNamespaces);
    }

    [TestMethod]
    public void ProcessPartialInterpretCallNode_ShouldReuseExistingInterpreterInstance()
    {
        // Arrange
        var dataSourceNode = new IdentifierNode("data", typeof(byte[]));
        var partialInterpretNode = new PartialInterpretCallNode(dataSourceNode, "BmpHeader");


        _interpreterInstances["BmpHeader"] = "_interpreter_BmpHeader";


        _nodes.Push(SyntaxFactory.IdentifierName("data"));

        // Act
        var result = InterpretCallNodeProcessor.ProcessPartialInterpretCallNode(
            partialInterpretNode,
            _generator,
            _nodes,
            _statements,
            _interpreterInstances,
            ns => _addedNamespaces.Add(ns));

        // Assert
        var invocation = result as InvocationExpressionSyntax;
        Assert.IsNotNull(invocation);
        Assert.IsEmpty(_statements);
    }

    [TestMethod]
    public void ProcessPartialInterpretCallNode_WithNullNode_ShouldThrow()
    {
        _nodes.Push(SyntaxFactory.IdentifierName("data"));


        Assert.Throws<ArgumentNullException>(() =>
            InterpretCallNodeProcessor.ProcessPartialInterpretCallNode(
                null,
                _generator,
                _nodes,
                _statements,
                _interpreterInstances,
                ns => _addedNamespaces.Add(ns)));
    }
}
