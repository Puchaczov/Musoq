using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

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

    private class TestSchema : SchemaBase
    {
        public TestSchema(string name, MethodsAggregator methodsAggregator) : base(name, methodsAggregator)
        {
        }
    }
}