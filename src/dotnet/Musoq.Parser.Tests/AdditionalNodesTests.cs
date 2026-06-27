using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

/// <summary>
///     Additional tests for Parser node classes focusing on ToString, Id, and properties
/// </summary>
[TestClass]
public partial class AdditionalNodesTests
{
    #region StringNode Tests

    [TestMethod]
    public void StringNode_Constructor_ShouldSetValue()
    {
        // Arrange & Act
        var node = new StringNode("test value");

        // Assert
        Assert.AreEqual("test value", node.Value);
    }

    [TestMethod]
    public void StringNode_ReturnType_ShouldBeString()
    {
        // Arrange & Act
        var node = new StringNode("test");

        // Assert
        Assert.AreEqual(typeof(string), node.ReturnType);
    }

    [TestMethod]
    public void StringNode_ObjValue_ShouldMatchValue()
    {
        // Arrange
        var value = "test string";
        var node = new StringNode(value);

        // Act & Assert
        Assert.AreEqual(value, node.ObjValue);
    }

    [TestMethod]
    public void StringNode_ToString_ShouldReturnQuotedValue()
    {
        // Arrange
        var node = new StringNode("hello");

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("'hello'", result);
    }

    [TestMethod]
    public void StringNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new StringNode("test");

        // Act & Assert
        Assert.Contains("StringNode", node.Id);
    }

    [TestMethod]
    public void StringNode_WithEmptyValue_ShouldWork()
    {
        // Arrange & Act
        var node = new StringNode(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, node.Value);
        Assert.AreEqual("''", node.ToString());
    }

    #endregion

    #region IntegerNode Tests

    [TestMethod]
    public void IntegerNode_WithInt_ShouldSetObjValue()
    {
        // Arrange & Act
        var node = new IntegerNode(42);

        // Assert
        Assert.AreEqual(42, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithLong_ShouldSetReturnType()
    {
        // Arrange & Act
        var node = new IntegerNode(999999999999L);

        // Assert
        Assert.AreEqual(typeof(long), node.ReturnType);
    }

    [TestMethod]
    public void IntegerNode_ToString_ShouldReturnValueAsString()
    {
        // Arrange
        var node = new IntegerNode(123);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("123", result);
    }

    [TestMethod]
    public void IntegerNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new IntegerNode(1);

        // Act & Assert
        Assert.Contains("IntegerNode", node.Id);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Byte_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("42", "b");

        // Assert
        Assert.IsInstanceOfType<sbyte>(node.ObjValue);
        Assert.AreEqual((sbyte)42, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedByte_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("200", "ub");

        // Assert
        Assert.IsInstanceOfType<byte>(node.ObjValue);
        Assert.AreEqual((byte)200, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Short_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("1000", "s");

        // Assert
        Assert.IsInstanceOfType<short>(node.ObjValue);
        Assert.AreEqual((short)1000, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedShort_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("50000", "us");

        // Assert
        Assert.IsInstanceOfType<ushort>(node.ObjValue);
        Assert.AreEqual((ushort)50000, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Int_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("100000", "i");

        // Assert
        Assert.IsInstanceOfType<int>(node.ObjValue);
        Assert.AreEqual(100000, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedInt_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("3000000000", "ui");

        // Assert
        Assert.IsInstanceOfType<uint>(node.ObjValue);
        Assert.AreEqual(3000000000u, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Long_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("9999999999", "l");

        // Assert
        Assert.IsInstanceOfType<long>(node.ObjValue);
        Assert.AreEqual(9999999999L, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedLong_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("18000000000000000000", "ul");

        // Assert
        Assert.IsInstanceOfType<ulong>(node.ObjValue);
        Assert.AreEqual(18000000000000000000ul, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndEmptyAbbreviation_ShouldAutoDetectType()
    {
        // Arrange & Act
        var node = new IntegerNode("42", "");

        // Assert - Should detect as int by default
        Assert.IsInstanceOfType<int>(node.ObjValue);
    }

    #endregion
}
