using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for SchemaRegistry and SchemaDefinitionVisitor.
/// </summary>
[TestClass]
public class SchemaRegistryTests
{
    #region SchemaRegistry Tests

    [TestMethod]
    public void Register_SingleSchema_ShouldSucceed()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var schemaNode = CreateTestBinarySchemaNode("TestSchema");

        // Act
        registry.Register("TestSchema", schemaNode);

        // Assert
        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("TestSchema"));
    }

    [TestMethod]
    public void Register_MultipleSchemas_ShouldSucceed()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var schema1 = CreateTestBinarySchemaNode("Schema1");
        var schema2 = CreateTestBinarySchemaNode("Schema2");
        var schema3 = CreateTestTextSchemaNode("Schema3");

        // Act
        registry.Register("Schema1", schema1);
        registry.Register("Schema2", schema2);
        registry.Register("Schema3", schema3);

        // Assert
        Assert.AreEqual(3, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("Schema1"));
        Assert.IsTrue(registry.ContainsSchema("Schema2"));
        Assert.IsTrue(registry.ContainsSchema("Schema3"));
    }

    [TestMethod]
    public void Register_DuplicateName_ShouldThrow()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var schema1 = CreateTestBinarySchemaNode("TestSchema");
        var schema2 = CreateTestBinarySchemaNode("TestSchema");
        registry.Register("TestSchema", schema1);


        Assert.Throws<InvalidOperationException>(() =>
            registry.Register("TestSchema", schema2));
    }

    [TestMethod]
    public void ContainsSchema_CaseInsensitive_ShouldMatch()
    {
        // Arrange
        var registry = new SchemaRegistry();
        registry.Register("TestSchema", CreateTestBinarySchemaNode("TestSchema"));


        Assert.IsTrue(registry.ContainsSchema("TestSchema"));
        Assert.IsTrue(registry.ContainsSchema("testschema"));
        Assert.IsTrue(registry.ContainsSchema("TESTSCHEMA"));
    }

    [TestMethod]
    public void GetSchema_ExistingSchema_ShouldReturn()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var schemaNode = CreateTestBinarySchemaNode("TestSchema");
        registry.Register("TestSchema", schemaNode);

        // Act
        var registration = registry.GetSchema("TestSchema");

        // Assert
        Assert.AreEqual("TestSchema", registration.Name);
        Assert.AreSame(schemaNode, registration.Node);
        Assert.IsTrue(registration.IsBinarySchema);
        Assert.IsFalse(registration.IsTextSchema);
    }

    [TestMethod]
    public void GetSchema_NonExistent_ShouldThrow()
    {
        var registry = new SchemaRegistry();


        Assert.Throws<KeyNotFoundException>(() =>
            registry.GetSchema("NonExistent"));
    }

    [TestMethod]
    public void TryGetSchema_ExistingSchema_ShouldReturnTrue()
    {
        // Arrange
        var registry = new SchemaRegistry();
        registry.Register("TestSchema", CreateTestBinarySchemaNode("TestSchema"));

        // Act
        var found = registry.TryGetSchema("TestSchema", out var registration);

        // Assert
        Assert.IsTrue(found);
        Assert.IsNotNull(registration);
        Assert.AreEqual("TestSchema", registration.Name);
    }

    [TestMethod]
    public void TryGetSchema_NonExistent_ShouldReturnFalse()
    {
        var registry = new SchemaRegistry();


        var found = registry.TryGetSchema("NonExistent", out var registration);


        Assert.IsFalse(found);
        Assert.IsNull(registration);
    }

    [TestMethod]
    public void Schemas_ShouldPreserveOrder()
    {
        // Arrange
        var registry = new SchemaRegistry();
        registry.Register("First", CreateTestBinarySchemaNode("First"));
        registry.Register("Second", CreateTestBinarySchemaNode("Second"));
        registry.Register("Third", CreateTestTextSchemaNode("Third"));

        // Act
        var schemas = registry.Schemas;

        // Assert
        Assert.HasCount(3, schemas);
        Assert.AreEqual("First", schemas[0].Name);
        Assert.AreEqual("Second", schemas[1].Name);
        Assert.AreEqual("Third", schemas[2].Name);
    }

    [TestMethod]
    public void Clear_ShouldRemoveAllSchemas()
    {
        // Arrange
        var registry = new SchemaRegistry();
        registry.Register("Schema1", CreateTestBinarySchemaNode("Schema1"));
        registry.Register("Schema2", CreateTestBinarySchemaNode("Schema2"));
        Assert.AreEqual(2, registry.Count);

        // Act
        registry.Clear();

        // Assert
        Assert.AreEqual(0, registry.Count);
        Assert.IsFalse(registry.ContainsSchema("Schema1"));
        Assert.IsFalse(registry.ContainsSchema("Schema2"));
    }

    #endregion

    #region SchemaRegistration Tests

    [TestMethod]
    public void SchemaRegistration_BinarySchema_ShouldBeIdentifiedCorrectly()
    {
        var node = CreateTestBinarySchemaNode("BinaryTest");
        var registration = new SchemaRegistration("BinaryTest", node);


        Assert.IsTrue(registration.IsBinarySchema);
        Assert.IsFalse(registration.IsTextSchema);
    }

    [TestMethod]
    public void SchemaRegistration_TextSchema_ShouldBeIdentifiedCorrectly()
    {
        var node = CreateTestTextSchemaNode("TextTest");
        var registration = new SchemaRegistration("TextTest", node);


        Assert.IsFalse(registration.IsBinarySchema);
        Assert.IsTrue(registration.IsTextSchema);
    }

    #endregion

    #region SchemaDefinitionVisitor Tests

    [TestMethod]
    public void Visitor_BinarySchema_ShouldRegister()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var schemaNode = CreateTestBinarySchemaNode("TestBinary");

        // Act
        visitor.Visit(schemaNode);

        // Assert
        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("TestBinary"));
    }

    [TestMethod]
    public void Visitor_TextSchema_ShouldRegister()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var schemaNode = CreateTestTextSchemaNode("TestText");

        // Act
        visitor.Visit(schemaNode);

        // Assert
        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("TestText"));
    }

    [TestMethod]
    public void Visitor_MultipleSchemas_ShouldRegisterAll()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);

        // Act
        visitor.Visit(CreateTestBinarySchemaNode("Binary1"));
        visitor.Visit(CreateTestBinarySchemaNode("Binary2"));
        visitor.Visit(CreateTestTextSchemaNode("Text1"));

        // Assert
        Assert.AreEqual(3, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("Binary1"));
        Assert.IsTrue(registry.ContainsSchema("Binary2"));
        Assert.IsTrue(registry.ContainsSchema("Text1"));
    }

    #endregion

    #region Helper Methods

    private static BinarySchemaNode CreateTestBinarySchemaNode(string name)
    {
        return new BinarySchemaNode(
            name,
            new[]
            {
                new FieldDefinitionNode(
                    "TestField",
                    new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian))
            });
    }

    private static TextSchemaNode CreateTestTextSchemaNode(string name)
    {
        return new TextSchemaNode(
            name,
            new[]
            {
                new TextFieldDefinitionNode(
                    "TestField",
                    TextFieldType.Rest)
            });
    }

    #endregion
}
