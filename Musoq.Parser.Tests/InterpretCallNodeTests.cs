using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class InterpretCallNodeTests
{
    [TestMethod]
    public void InterpretCallNode_ShouldStoreDataSourceAndSchemaName()
    {
        var dataSource = new IdentifierNode("data", typeof(byte[]));
        var schemaName = "Header";


        var node = new InterpretCallNode(dataSource, schemaName);


        Assert.AreEqual(dataSource, node.DataSource);
        Assert.AreEqual(schemaName, node.SchemaName);
        Assert.IsNull(node.ReturnType);
    }

    [TestMethod]
    public void InterpretCallNode_WithReturnType_ShouldStoreAllProperties()
    {
        var dataSource = new IdentifierNode("data", typeof(byte[]));
        var schemaName = "Header";
        var returnType = typeof(object);


        var node = new InterpretCallNode(dataSource, schemaName, returnType);


        Assert.AreEqual(dataSource, node.DataSource);
        Assert.AreEqual(schemaName, node.SchemaName);
        Assert.AreEqual(returnType, node.ReturnType);
    }

    [TestMethod]
    public void InterpretCallNode_ToString_ShouldReturnExpectedFormat()
    {
        // Arrange
        var dataSource = new IdentifierNode("f.Content", typeof(byte[]));
        var schemaName = "BmpHeader";
        var node = new InterpretCallNode(dataSource, schemaName);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("Interpret(f.Content, BmpHeader)", result);
    }

    [TestMethod]
    public void InterpretCallNode_Id_ShouldBeUnique()
    {
        var dataSource1 = new IdentifierNode("data1", typeof(byte[]));
        var dataSource2 = new IdentifierNode("data2", typeof(byte[]));
        var node1 = new InterpretCallNode(dataSource1, "Schema1");
        var node2 = new InterpretCallNode(dataSource2, "Schema2");
        var node3 = new InterpretCallNode(dataSource1, "Schema1");


        Assert.AreNotEqual(node1.Id, node2.Id);
        Assert.AreEqual(node1.Id, node3.Id);
    }

    [TestMethod]
    public void ParseCallNode_ShouldStoreDataSourceAndSchemaName()
    {
        var dataSource = new IdentifierNode("line", typeof(string));
        var schemaName = "LogEntry";


        var node = new ParseCallNode(dataSource, schemaName);


        Assert.AreEqual(dataSource, node.DataSource);
        Assert.AreEqual(schemaName, node.SchemaName);
        Assert.IsNull(node.ReturnType);
    }

    [TestMethod]
    public void ParseCallNode_WithReturnType_ShouldStoreAllProperties()
    {
        var dataSource = new IdentifierNode("line", typeof(string));
        var schemaName = "LogEntry";
        var returnType = typeof(object);


        var node = new ParseCallNode(dataSource, schemaName, returnType);


        Assert.AreEqual(dataSource, node.DataSource);
        Assert.AreEqual(schemaName, node.SchemaName);
        Assert.AreEqual(returnType, node.ReturnType);
    }

    [TestMethod]
    public void ParseCallNode_ToString_ShouldReturnExpectedFormat()
    {
        // Arrange
        var dataSource = new IdentifierNode("line.Value", typeof(string));
        var schemaName = "CsvRow";
        var node = new ParseCallNode(dataSource, schemaName);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("Parse(line.Value, CsvRow)", result);
    }

    [TestMethod]
    public void InterpretAtCallNode_ShouldStoreAllProperties()
    {
        var dataSource = new IdentifierNode("data", typeof(byte[]));
        var offset = new IntegerNode("100", "s");
        var schemaName = "Record";


        var node = new InterpretAtCallNode(dataSource, offset, schemaName);


        Assert.AreEqual(dataSource, node.DataSource);
        Assert.AreEqual(offset, node.Offset);
        Assert.AreEqual(schemaName, node.SchemaName);
        Assert.IsNull(node.ReturnType);
    }

    [TestMethod]
    public void InterpretAtCallNode_WithReturnType_ShouldStoreAllProperties()
    {
        var dataSource = new IdentifierNode("data", typeof(byte[]));
        var offset = new IntegerNode("50", "s");
        var schemaName = "Record";
        var returnType = typeof(object);


        var node = new InterpretAtCallNode(dataSource, offset, schemaName, returnType);


        Assert.AreEqual(dataSource, node.DataSource);
        Assert.AreEqual(offset, node.Offset);
        Assert.AreEqual(schemaName, node.SchemaName);
        Assert.AreEqual(returnType, node.ReturnType);
    }

    [TestMethod]
    public void InterpretAtCallNode_ToString_ShouldReturnExpectedFormat()
    {
        // Arrange
        var dataSource = new IdentifierNode("buffer", typeof(byte[]));
        var offset = new IntegerNode("256", "s");
        var schemaName = "Packet";
        var node = new InterpretAtCallNode(dataSource, offset, schemaName);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("InterpretAt(buffer, 256, Packet)", result);
    }

    [TestMethod]
    public void InterpretCallNode_NullDataSource_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new InterpretCallNode(null!, "Schema"));
    }

    [TestMethod]
    public void InterpretCallNode_NullSchemaName_ShouldThrow()
    {
        var dataSource = new IdentifierNode("data", typeof(byte[]));


        Assert.Throws<ArgumentNullException>(() => new InterpretCallNode(dataSource, null!));
    }

    [TestMethod]
    public void ParseCallNode_NullDataSource_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ParseCallNode(null!, "Schema"));
    }

    [TestMethod]
    public void InterpretAtCallNode_NullOffset_ShouldThrow()
    {
        // Arrange
        var dataSource = new IdentifierNode("data", typeof(byte[]));


        Assert.Throws<ArgumentNullException>(() => new InterpretAtCallNode(dataSource, null!, "Schema"));
    }
}
