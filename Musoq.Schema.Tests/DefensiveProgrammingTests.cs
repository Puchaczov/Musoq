using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
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
        var exception = Assert.Throws<ParserValidationException>(() => new Lexer(null, true));
        Assert.Contains("cannot be null", exception.Message);
        Assert.Contains("valid SQL query", exception.Message);
    }

    [TestMethod]
    public void Parser_Should_ThrowMeaningfulException_WhenEmptyInput()
    {
        // Act & Assert
        var exception = Assert.Throws<ParserValidationException>(() => new Lexer("", true));
        Assert.Contains("cannot be empty", exception.Message);
        Assert.Contains("valid SQL query", exception.Message);
    }

    [TestMethod]
    public void Parser_Should_ThrowMeaningfulException_WhenWhitespaceInput()
    {
        // Act & Assert
        var exception = Assert.Throws<ParserValidationException>(() => new Lexer("   ", true));
        Assert.Contains("cannot be empty", exception.Message);
        Assert.Contains("whitespace", exception.Message);
    }

    [TestMethod]
    public void SchemaBase_Should_ThrowMeaningfulException_WhenNullName()
    {
        // Arrange
        var methodManager = new MethodsManager();
        var aggregator = new MethodsAggregator(methodManager);

        // Act & Assert
        var exception = Assert.Throws<SchemaArgumentException>(() => new TestSchema(null, aggregator));
        Assert.Contains("cannot be empty", exception.Message);
        Assert.Contains("initializing a schema", exception.Message);
        Assert.AreEqual("name", exception.ParamName);
    }

    [TestMethod]
    public void SchemaBase_Should_ThrowMeaningfulException_WhenNullMethodsAggregator()
    {
        // Act & Assert
        var exception = Assert.Throws<SchemaArgumentException>(() => new TestSchema("test", null));
        Assert.Contains("cannot be null", exception.Message);
        Assert.Contains("initializing a schema", exception.Message);
        Assert.AreEqual("methodsAggregator", exception.ParamName);
    }

    [TestMethod]
    public void MethodsMetadata_Should_ThrowMeaningfulException_WhenNullMethodName()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        var exception = Assert.Throws<SchemaArgumentException>(() => 
            metadata.GetMethod(null, new Type[0], null));
        Assert.Contains("cannot be empty", exception.Message);
        Assert.Contains("resolving a method", exception.Message);
        Assert.AreEqual("name", exception.ParamName);
    }

    [TestMethod]
    public void MethodsMetadata_Should_ThrowMeaningfulException_WhenNullMethodArgs()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        var exception = Assert.Throws<SchemaArgumentException>(() => 
            metadata.GetMethod("test", null, null));
        Assert.Contains("cannot be null", exception.Message);
        Assert.Contains("resolving a method", exception.Message);
        Assert.AreEqual("methodArgs", exception.ParamName);
    }

    [TestMethod]
    public void CloneQueryVisitor_Should_ThrowMeaningfulException_WhenStackEmpty()
    {
        // Arrange
        var visitor = new CloneQueryVisitor();

        // Act & Assert
        var exception = Assert.Throws<VisitorException>(() => visitor.Root);
        Assert.Contains("Stack underflow", exception.Message);
        Assert.Contains("CloneQueryVisitor", exception.Message);
        Assert.AreEqual("CloneQueryVisitor", exception.VisitorName);
    }

    [TestMethod]
    public void DefensiveVisitorBase_Should_ThrowMeaningfulException_WhenStackUnderflow()
    {
        // Arrange
        var visitor = new TestDefensiveVisitor();
        var nodes = new Stack<Node>();

        // Act & Assert
        var exception = Assert.Throws<VisitorException>(() => visitor.TestSafePop(nodes));
        Assert.Contains("Stack underflow", exception.Message);
        Assert.Contains("Expected at least 1 item", exception.Message);
        Assert.Contains("found 0", exception.Message);
    }

    [TestMethod]
    public void DefensiveVisitorBase_Should_ThrowMeaningfulException_WhenInvalidNodeType()
    {
        // Arrange
        var visitor = new TestDefensiveVisitor();
        var stringNode = new StringNode("test");

        // Act & Assert
        var exception = Assert.Throws<VisitorException>(() => visitor.TestSafeCast<IntegerNode>(stringNode));
        Assert.Contains("Invalid node type", exception.Message);
        Assert.Contains("Expected 'IntegerNode'", exception.Message);
        Assert.Contains("got 'StringNode'", exception.Message);
    }

    [TestMethod]
    public void DefensiveVisitorBase_Should_ThrowMeaningfulException_WhenNullNode()
    {
        // Arrange
        var visitor = new TestDefensiveVisitor();

        // Act & Assert
        var exception = Assert.Throws<VisitorException>(() => visitor.TestSafeCast<StringNode>(null));
        Assert.Contains("Expected 'StringNode' node but received null", exception.Message);
        Assert.Contains("AST processing error", exception.Message);
    }

    [TestMethod]
    public void ToCSharpRewriteTreeVisitor_Should_ThrowMeaningfulException_WhenNullAssemblies()
    {
        // Act & Assert
        var exception = Assert.Throws<VisitorException>(() => 
            new ToCSharpRewriteTreeVisitor(null, new Dictionary<string, int[]>(), new Dictionary<SchemaFromNode, ISchemaColumn[]>(), "test", new CompilationOptions()));
        Assert.Contains("cannot be null", exception.Message);
        Assert.Contains("ToCSharpRewriteTreeVisitor", exception.Message);
    }

    [TestMethod]
    public void ToCSharpRewriteTreeVisitor_Should_ThrowMeaningfulException_WhenEmptyAssemblyName()
    {
        // Act & Assert
        var exception = Assert.Throws<VisitorException>(() => 
            new ToCSharpRewriteTreeVisitor(new System.Reflection.Assembly[0], new Dictionary<string, int[]>(), new Dictionary<SchemaFromNode, ISchemaColumn[]>(), "", new CompilationOptions()));
        Assert.Contains("cannot be null or empty", exception.Message);
        Assert.Contains("assemblyName", exception.Message);
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