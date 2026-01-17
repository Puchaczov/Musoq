using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for Parser nodes with 0% coverage (Session 2 - Phase 2)
/// </summary>
[TestClass]
public class ZeroCoverageNodesTests
{
    #region AccessRawIdentifierNode Tests
    
    [TestMethod]
    public void AccessRawIdentifierNode_Constructor_ShouldSetName()
    {
        // Arrange & Act
        var node = new AccessRawIdentifierNode("myIdentifier");
        
        // Assert
        Assert.AreEqual("myIdentifier", node.Name);
    }
    
    [TestMethod]
    public void AccessRawIdentifierNode_WithReturnType_ShouldSetType()
    {
        // Arrange & Act
        var node = new AccessRawIdentifierNode("myIdentifier", typeof(int));
        
        // Assert
        Assert.AreEqual(typeof(int), node.ReturnType);
    }
    
    [TestMethod]
    public void AccessRawIdentifierNode_ToString_ShouldReturnName()
    {
        // Arrange
        var node = new AccessRawIdentifierNode("testName");
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.AreEqual("testName", result);
    }
    
    [TestMethod]
    public void AccessRawIdentifierNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new AccessRawIdentifierNode("test");
        
        // Act & Assert
        Assert.Contains("IdentifierNode", node.Id);
    }
    
    #endregion
    
    #region GroupSelectNode Tests
    
    [TestMethod]
    public void GroupSelectNode_Constructor_ShouldSetFields()
    {
        // Arrange
        var fields = new FieldNode[]
        {
            new FieldNode(new IntegerNode(1), 0, "col1")
        };
        
        // Act
        var node = new GroupSelectNode(fields);
        
        // Assert
        Assert.HasCount(1, node.Fields);
    }
    
    [TestMethod]
    public void GroupSelectNode_WithEmptyFields_ShouldWork()
    {
        // Arrange & Act
        var node = new GroupSelectNode(Array.Empty<FieldNode>());
        
        // Assert
        Assert.IsEmpty(node.Fields);
    }
    
    [TestMethod]
    public void GroupSelectNode_IsSelectNode()
    {
        // Arrange & Act
        var node = new GroupSelectNode(Array.Empty<FieldNode>());
        
        // Assert
        Assert.IsInstanceOfType(node, typeof(SelectNode));
    }
    
    #endregion
    
    #region IntoNode Tests
    
    [TestMethod]
    public void IntoNode_Constructor_ShouldSetName()
    {
        // Arrange & Act
        var node = new IntoNode("targetTable");
        
        // Assert
        Assert.AreEqual("targetTable", node.Name);
    }
    
    [TestMethod]
    public void IntoNode_ReturnType_ShouldBeNull()
    {
        // Arrange & Act
        var node = new IntoNode("test");
        
        // Assert
        Assert.IsNull(node.ReturnType);
    }
    
    [TestMethod]
    public void IntoNode_Id_ShouldContainNodeNameAndTableName()
    {
        // Arrange
        var node = new IntoNode("myTable");
        
        // Act & Assert
        Assert.Contains("IntoNode", node.Id);
        Assert.Contains("myTable", node.Id);
    }
    
    [TestMethod]
    public void IntoNode_ToString_ShouldReturnIntoStatement()
    {
        // Arrange
        var node = new IntoNode("results");
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.AreEqual("into results", result);
    }
    
    #endregion
    
    #region PutTrueNode Tests
    
    [TestMethod]
    public void PutTrueNode_ReturnType_ShouldBeBool()
    {
        // Arrange & Act
        var node = new PutTrueNode();
        
        // Assert
        Assert.AreEqual(typeof(bool), node.ReturnType);
    }
    
    [TestMethod]
    public void PutTrueNode_Id_ShouldContainNodeName()
    {
        // Arrange & Act
        var node = new PutTrueNode();
        
        // Assert
        Assert.Contains("PutTrueNode", node.Id);
    }
    
    [TestMethod]
    public void PutTrueNode_ToString_ShouldReturnTrueCondition()
    {
        // Arrange
        var node = new PutTrueNode();
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.AreEqual("1 = 1", result);
    }
    
    #endregion
    
    #region QueryScope Tests
    
    [TestMethod]
    public void QueryScope_Constructor_ShouldSetStatements()
    {
        // Arrange
        var statements = new Node[]
        {
            new IntegerNode(1),
            new StringNode("test")
        };
        
        // Act
        var node = new QueryScope(statements);
        
        // Assert
        Assert.HasCount(2, node.Statements);
    }
    
    [TestMethod]
    public void QueryScope_WithEmptyStatements_ShouldWork()
    {
        // Arrange & Act
        var node = new QueryScope(Array.Empty<Node>());
        
        // Assert
        Assert.IsEmpty(node.Statements);
    }
    
    [TestMethod]
    public void QueryScope_ToString_ShouldReturnNull()
    {
        // Arrange
        var node = new QueryScope(Array.Empty<Node>());
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region RawFunctionNode Tests
    
    [TestMethod]
    public void RawFunctionNode_Constructor_ShouldSetParameters()
    {
        // Arrange
        var args = new Node[]
        {
            new IntegerNode(1),
            new StringNode("test")
        };
        
        // Act
        var node = new RawFunctionNode(args);
        
        // Assert
        Assert.HasCount(2, node.Parameters);
    }
    
    [TestMethod]
    public void RawFunctionNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new RawFunctionNode(new Node[] { new IntegerNode(1) });
        
        // Act & Assert
        Assert.Contains("RawFunctionNode", node.Id);
    }
    
    [TestMethod]
    public void RawFunctionNode_ToString_ShouldReturnNull()
    {
        // Arrange
        var node = new RawFunctionNode(new Node[] { new IntegerNode(1) });
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region ShortCircuitingNodeLeft Tests
    
    [TestMethod]
    public void ShortCircuitingNodeLeft_Constructor_ShouldSetProperties()
    {
        // Arrange
        var expression = new BooleanNode(true);
        
        // Act
        var node = new ShortCircuitingNodeLeft(expression, TokenType.And);
        
        // Assert
        Assert.AreSame(expression, node.Expression);
        Assert.AreEqual(TokenType.And, node.UsedFor);
    }
    
    [TestMethod]
    public void ShortCircuitingNodeLeft_ReturnType_ShouldMatchExpression()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new ShortCircuitingNodeLeft(expression, TokenType.Or);
        
        // Act & Assert
        Assert.AreEqual(typeof(bool), node.ReturnType);
    }
    
    [TestMethod]
    public void ShortCircuitingNodeLeft_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new ShortCircuitingNodeLeft(new BooleanNode(true), TokenType.And);
        
        // Act & Assert
        Assert.Contains("ShortCircuitingNodeLeft", node.Id);
    }
    
    [TestMethod]
    public void ShortCircuitingNodeLeft_ToString_ShouldReturnExpressionString()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new ShortCircuitingNodeLeft(expression, TokenType.And);
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.AreEqual("true", result);
    }
    
    #endregion
    
    #region ShortCircuitingNodeRight Tests
    
    [TestMethod]
    public void ShortCircuitingNodeRight_Constructor_ShouldSetProperties()
    {
        // Arrange
        var expression = new BooleanNode(false);
        
        // Act
        var node = new ShortCircuitingNodeRight(expression, TokenType.Or);
        
        // Assert
        Assert.AreSame(expression, node.Expression);
        Assert.AreEqual(TokenType.Or, node.UsedFor);
    }
    
    [TestMethod]
    public void ShortCircuitingNodeRight_ReturnType_ShouldMatchExpression()
    {
        // Arrange
        var expression = new IntegerNode(42);
        var node = new ShortCircuitingNodeRight(expression, TokenType.And);
        
        // Act & Assert
        Assert.AreEqual(typeof(int), node.ReturnType);
    }
    
    [TestMethod]
    public void ShortCircuitingNodeRight_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new ShortCircuitingNodeRight(new BooleanNode(false), TokenType.Or);
        
        // Act & Assert
        Assert.Contains("ShortCircuitingNodeRight", node.Id);
    }
    
    [TestMethod]
    public void ShortCircuitingNodeRight_ToString_ShouldReturnExpressionString()
    {
        // Arrange
        var expression = new BooleanNode(false);
        var node = new ShortCircuitingNodeRight(expression, TokenType.Or);
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.AreEqual("false", result);
    }
    
    #endregion
    
    #region ShouldBePresentInTheTable Tests
    
    [TestMethod]
    public void ShouldBePresentInTheTable_Constructor_ShouldSetProperties()
    {
        // Arrange
        var table = "users";
        var keys = new[] { "id", "name" };
        
        // Act
        var node = new ShouldBePresentInTheTable(table, true, keys);
        
        // Assert
        Assert.AreEqual(table, node.Table);
        Assert.IsTrue(node.ExpectedResult);
        Assert.HasCount(2, node.Keys);
    }
    
    [TestMethod]
    public void ShouldBePresentInTheTable_ReturnType_ShouldBeNull()
    {
        // Arrange & Act
        var node = new ShouldBePresentInTheTable("test", false, Array.Empty<string>());
        
        // Assert
        Assert.IsNull(node.ReturnType);
    }
    
    [TestMethod]
    public void ShouldBePresentInTheTable_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new ShouldBePresentInTheTable("test", false, new[] { "key" });
        
        // Act & Assert
        Assert.Contains("ShouldBePresentInTheTable", node.Id);
    }
    
    [TestMethod]
    public void ShouldBePresentInTheTable_WithEmptyKeys_ShouldWork()
    {
        // Arrange & Act
        var node = new ShouldBePresentInTheTable("test", true, Array.Empty<string>());
        
        // Assert
        Assert.IsEmpty(node.Keys);
        Assert.IsNotNull(node.Id);
    }
    
    [TestMethod]
    public void ShouldBePresentInTheTable_ToString_ShouldContainTableName()
    {
        // Arrange
        var node = new ShouldBePresentInTheTable("orders", false, new[] { "orderId" });
        
        // Act
        var result = node.ToString();
        
        // Assert
        Assert.Contains("orders", result);
        Assert.Contains("Should be present", result);
    }
    
    #endregion
    
    #region TranslatedSetTreeNode Tests
    
    [TestMethod]
    public void TranslatedSetTreeNode_WithEmptyNodes_ShouldWork()
    {
        // Arrange & Act
        var node = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>());
        
        // Assert
        Assert.IsEmpty(node.Nodes);
    }
    
    [TestMethod]
    public void TranslatedSetTreeNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>());
        
        // Act & Assert
        Assert.Contains("TranslatedSetTreeNode", node.Id);
    }
    
    #endregion
    
    #region AccessCallChainNode Tests
    
    [TestMethod]
    public void AccessCallChainNode_Constructor_ShouldSetProperties()
    {
        // Arrange
        var propInfo = typeof(string).GetProperty(nameof(string.Length));
        var props = new (PropertyInfo, object)[] { (propInfo, null) };
        
        // Act
        var node = new AccessCallChainNode("column", typeof(string), props, "alias");
        
        // Assert
        Assert.AreEqual("column", node.ColumnName);
        Assert.AreEqual(typeof(string), node.ColumnType);
        Assert.AreEqual("alias", node.Alias);
        Assert.HasCount(1, node.Props);
    }
    
    [TestMethod]
    public void AccessCallChainNode_ReturnType_WithNullArg_ShouldReturnPropertyType()
    {
        // Arrange
        var propInfo = typeof(string).GetProperty(nameof(string.Length));
        var props = new (PropertyInfo, object)[] { (propInfo, null) };
        var node = new AccessCallChainNode("column", typeof(string), props, "alias");
        
        // Act
        var returnType = node.ReturnType;
        
        // Assert
        Assert.AreEqual(typeof(int), returnType);
    }
    
    [TestMethod]
    public void AccessCallChainNode_ReturnType_WithArg_ShouldReturnElementType()
    {
        // Arrange
        // Use an array property to test element type
        var propInfo = typeof(TestArrayClass).GetProperty(nameof(TestArrayClass.Items));
        var props = new (PropertyInfo, object)[] { (propInfo, 0) }; // With arg (index)
        var node = new AccessCallChainNode("column", typeof(TestArrayClass), props, "alias");
        
        // Act
        var returnType = node.ReturnType;
        
        // Assert
        Assert.AreEqual(typeof(string), returnType);
    }
    
    [TestMethod]
    public void AccessCallChainNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var propInfo = typeof(string).GetProperty(nameof(string.Length));
        var props = new (PropertyInfo, object)[] { (propInfo, null) };
        var node = new AccessCallChainNode("col", typeof(string), props, "a");
        
        // Act & Assert
        Assert.Contains("AccessCallChainNode", node.Id);
    }
    
    #endregion
    
    #region Helper Classes
    
    private class TestArrayClass
    {
        public string[] Items { get; set; }
    }
    
    #endregion
}
