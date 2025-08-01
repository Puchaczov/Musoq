using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;
using System.Collections.Generic;

namespace Musoq.Schema.Tests.DefensiveProgramming;

[TestClass]
public class DefensiveProgrammingTests
{
    [TestMethod]
    public void Parser_Should_ThrowMeaningfulException_WhenNullInput()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ParserValidationException>(() => new Lexer(null, true));
        Assert.IsTrue(exception.Message.Contains("cannot be null"));
        Assert.IsTrue(exception.Message.Contains("valid SQL query"));
    }

    [TestMethod]
    public void Parser_Should_ThrowMeaningfulException_WhenEmptyInput()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ParserValidationException>(() => new Lexer("", true));
        Assert.IsTrue(exception.Message.Contains("cannot be empty"));
        Assert.IsTrue(exception.Message.Contains("valid SQL query"));
    }

    [TestMethod]
    public void Parser_Should_ThrowMeaningfulException_WhenWhitespaceInput()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ParserValidationException>(() => new Lexer("   ", true));
        Assert.IsTrue(exception.Message.Contains("cannot be empty"));
        Assert.IsTrue(exception.Message.Contains("whitespace"));
    }

    [TestMethod]
    public void SchemaBase_Should_ThrowMeaningfulException_WhenNullName()
    {
        // Arrange
        var methodManager = new MethodsManager();
        var aggregator = new MethodsAggregator(methodManager);

        // Act & Assert
        var exception = Assert.ThrowsException<SchemaArgumentException>(() => new TestSchema(null, aggregator));
        Assert.IsTrue(exception.Message.Contains("cannot be empty"));
        Assert.IsTrue(exception.Message.Contains("initializing a schema"));
        Assert.AreEqual("name", exception.ParamName);
    }

    [TestMethod]
    public void SchemaBase_Should_ThrowMeaningfulException_WhenNullMethodsAggregator()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<SchemaArgumentException>(() => new TestSchema("test", null));
        Assert.IsTrue(exception.Message.Contains("cannot be null"));
        Assert.IsTrue(exception.Message.Contains("initializing a schema"));
        Assert.AreEqual("methodsAggregator", exception.ParamName);
    }

    [TestMethod]
    public void MethodsMetadata_Should_ThrowMeaningfulException_WhenNullMethodName()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        var exception = Assert.ThrowsException<SchemaArgumentException>(() => 
            metadata.GetMethod(null, new Type[0], null));
        Assert.IsTrue(exception.Message.Contains("cannot be empty"));
        Assert.IsTrue(exception.Message.Contains("resolving a method"));
        Assert.AreEqual("name", exception.ParamName);
    }

    [TestMethod]
    public void MethodsMetadata_Should_ThrowMeaningfulException_WhenNullMethodArgs()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        var exception = Assert.ThrowsException<SchemaArgumentException>(() => 
            metadata.GetMethod("test", null, null));
        Assert.IsTrue(exception.Message.Contains("cannot be null"));
        Assert.IsTrue(exception.Message.Contains("resolving a method"));
        Assert.AreEqual("methodArgs", exception.ParamName);
    }

    [TestMethod]
    public void CloneQueryVisitor_Should_ThrowMeaningfulException_WhenStackEmpty()
    {
        // Arrange
        var visitor = new CloneQueryVisitor();

        // Act & Assert
        var exception = Assert.ThrowsException<VisitorException>(() => visitor.Root);
        Assert.IsTrue(exception.Message.Contains("Stack underflow"));
        Assert.IsTrue(exception.Message.Contains("CloneQueryVisitor"));
        Assert.AreEqual("CloneQueryVisitor", exception.VisitorName);
    }

    [TestMethod]
    public void DefensiveVisitorBase_Should_ThrowMeaningfulException_WhenStackUnderflow()
    {
        // Arrange
        var visitor = new TestDefensiveVisitor();
        var nodes = new Stack<Node>();

        // Act & Assert
        var exception = Assert.ThrowsException<VisitorException>(() => visitor.TestSafePop(nodes));
        Assert.IsTrue(exception.Message.Contains("Stack underflow"));
        Assert.IsTrue(exception.Message.Contains("Expected at least 1 item"));
        Assert.IsTrue(exception.Message.Contains("found 0"));
    }

    [TestMethod]
    public void DefensiveVisitorBase_Should_ThrowMeaningfulException_WhenInvalidNodeType()
    {
        // Arrange
        var visitor = new TestDefensiveVisitor();
        var stringNode = new StringNode("test");

        // Act & Assert
        var exception = Assert.ThrowsException<VisitorException>(() => visitor.TestSafeCast<IntegerNode>(stringNode));
        Assert.IsTrue(exception.Message.Contains("Invalid node type"));
        Assert.IsTrue(exception.Message.Contains("Expected 'IntegerNode'"));
        Assert.IsTrue(exception.Message.Contains("got 'StringNode'"));
    }

    [TestMethod]
    public void DefensiveVisitorBase_Should_ThrowMeaningfulException_WhenNullNode()
    {
        // Arrange
        var visitor = new TestDefensiveVisitor();

        // Act & Assert
        var exception = Assert.ThrowsException<VisitorException>(() => visitor.TestSafeCast<StringNode>(null));
        Assert.IsTrue(exception.Message.Contains("Expected 'StringNode' node but received null"));
        Assert.IsTrue(exception.Message.Contains("AST processing error"));
    }

    [TestMethod]
    public void ToCSharpRewriteTreeVisitor_Should_ThrowMeaningfulException_WhenNullAssemblies()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<VisitorException>(() => 
            new ToCSharpRewriteTreeVisitor(null, new Dictionary<string, int[]>(), new Dictionary<SchemaFromNode, ISchemaColumn[]>(), "test"));
        Assert.IsTrue(exception.Message.Contains("cannot be null"));
        Assert.IsTrue(exception.Message.Contains("ToCSharpRewriteTreeVisitor"));
    }

    [TestMethod]
    public void ToCSharpRewriteTreeVisitor_Should_ThrowMeaningfulException_WhenEmptyAssemblyName()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<VisitorException>(() => 
            new ToCSharpRewriteTreeVisitor(new System.Reflection.Assembly[0], new Dictionary<string, int[]>(), new Dictionary<SchemaFromNode, ISchemaColumn[]>(), ""));
        Assert.IsTrue(exception.Message.Contains("cannot be null or empty"));
        Assert.IsTrue(exception.Message.Contains("assemblyName"));
    }

    private class TestSchema : SchemaBase
    {
        public TestSchema(string name, MethodsAggregator methodsAggregator) : base(name, methodsAggregator)
        {
        }
    }

    private class TestDefensiveVisitor : DefensiveVisitorBase
    {
        protected override string VisitorName => "TestDefensiveVisitor";

        public Node TestSafePop(Stack<Node> nodes) => SafePop(nodes, "TestOperation");
        public T TestSafeCast<T>(Node node) where T : Node => SafeCast<T>(node, "TestOperation");
    }
}