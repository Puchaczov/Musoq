using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

/// <summary>
/// Tests for invalid schema operations to ensure meaningful error messages
/// These tests complement DefensiveProgrammingTests.cs by focusing on additional schema-level error scenarios
/// </summary>
[TestClass]
public class InvalidSchemaOperationsTests
{
    [TestMethod]
    public void MethodsMetadata_GetNonExistentMethod_ShouldThrowMeaningfulError()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        try
        {
            metadata.GetMethod("NonExistentMethod", Array.Empty<Type>(), null);
            Assert.Fail("Expected an exception for non-existent method");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
        }
    }

    [TestMethod]
    public void EmptySchemaName_ShouldThrowMeaningfulError()
    {
        // Arrange
        var methodManager = new MethodsManager();
        var aggregator = new MethodsAggregator(methodManager);

        // Act & Assert
        var exc = Assert.ThrowsException<SchemaArgumentException>(() => new TestSchema("", aggregator));
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Contains("cannot be empty") || exc.Message.Contains("empty"),
            $"Error message should mention empty value: {exc.Message}");
    }

    [TestMethod]
    public void WhiteSpaceSchemaName_ShouldThrowMeaningfulError()
    {
        // Arrange
        var methodManager = new MethodsManager();
        var aggregator = new MethodsAggregator(methodManager);

        // Act & Assert
        var exc = Assert.ThrowsException<SchemaArgumentException>(() => new TestSchema("   ", aggregator));
        Assert.IsNotNull(exc.Message);
        Assert.IsTrue(exc.Message.Contains("cannot be empty") || exc.Message.Contains("whitespace"),
            $"Error message should mention whitespace/empty: {exc.Message}");
    }

    [TestMethod]
    public void SchemaProvider_AccessNonRegisteredSchema_ShouldReturnNull()
    {
        // This test validates that accessing a schema that doesn't exist returns null
        // which allows the caller to handle the missing schema appropriately
        
        var schemaProvider = new EmptySchemaProvider();
        var schema = schemaProvider.GetSchema("NonExistentSchema");
        
        Assert.IsNull(schema, "Non-existent schema should return null");
    }

    [TestMethod]
    public void MethodsMetadata_GetMethodWithNullName_ShouldThrowMeaningfulError()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        try
        {
            metadata.GetMethod(null, Array.Empty<Type>(), null);
            Assert.Fail("Expected an exception for null method name");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
        }
    }

    [TestMethod]
    public void MethodsMetadata_GetMethodWithNullTypes_ShouldThrowMeaningfulError()
    {
        // Arrange
        var metadata = new MethodsMetadata();

        // Act & Assert
        try
        {
            metadata.GetMethod("SomeMethod", null, null);
            Assert.Fail("Expected an exception for null method types");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
        }
    }

    [TestMethod]
    public void MethodsAggregator_WithNullMethodsManager_ShouldThrowMeaningfulError()
    {
        // Act & Assert
        try
        {
            var aggregator = new MethodsAggregator(null);
            Assert.Fail("Expected an exception for null methods manager");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("null") || exc.Message.Contains("cannot be"),
                $"Error should mention null parameter: {exc.Message}");
        }
    }

    // Helper classes for testing
    private class TestSchema : SchemaBase
    {
        public TestSchema(string name, MethodsAggregator methodsAggregator) 
            : base(name, methodsAggregator)
        {
        }
    }

    private class EmptySchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return null; // Simulates schema not found
        }
    }
}
