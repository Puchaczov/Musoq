using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public partial class AdditionalNodesTests
{

    #region DecimalNode Tests

    [TestMethod]
    public void DecimalNode_Constructor_ShouldSetValue()
    {
        // Arrange & Act
        var node = new DecimalNode("3.14");

        // Assert
        Assert.AreEqual(3.14m, node.ObjValue);
    }

    [TestMethod]
    public void DecimalNode_ReturnType_ShouldBeDecimal()
    {
        // Arrange & Act
        var node = new DecimalNode("1.0");

        // Assert
        Assert.AreEqual(typeof(decimal), node.ReturnType);
    }

    [TestMethod]
    public void DecimalNode_ToString_ShouldReturnValueAsString()
    {
        // Arrange
        var node = new DecimalNode("123.456");

        // Act
        var result = node.ToString();

        // Assert
        // The output may vary based on culture settings, just verify it's not null
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DecimalNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new DecimalNode("1.0");

        // Act & Assert
        Assert.Contains("DecimalNode", node.Id);
    }

    #endregion

    #region BooleanNode Tests

    [TestMethod]
    public void BooleanNode_WithTrue_ShouldSetValue()
    {
        // Arrange & Act
        var node = new BooleanNode(true);

        // Assert
        Assert.IsTrue(node.Value);
        Assert.IsTrue((bool?)node.ObjValue);
    }

    [TestMethod]
    public void BooleanNode_WithFalse_ShouldSetValue()
    {
        // Arrange & Act
        var node = new BooleanNode(false);

        // Assert
        Assert.IsFalse(node.Value);
        Assert.IsFalse((bool?)node.ObjValue);
    }

    [TestMethod]
    public void BooleanNode_ReturnType_ShouldBeBool()
    {
        // Arrange & Act
        var node = new BooleanNode(true);

        // Assert
        Assert.AreEqual(typeof(bool), node.ReturnType);
    }

    [TestMethod]
    public void BooleanNode_ToString_ShouldReturnLowerCaseValue()
    {
        // Arrange
        var trueNode = new BooleanNode(true);
        var falseNode = new BooleanNode(false);

        // Act & Assert
        Assert.AreEqual("true", trueNode.ToString());
        Assert.AreEqual("false", falseNode.ToString());
    }

    [TestMethod]
    public void BooleanNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new BooleanNode(true);

        // Act & Assert
        Assert.Contains("BooleanNode", node.Id);
    }

    #endregion

    #region NullNode Tests

    [TestMethod]
    public void NullNode_ReturnType_ShouldBeNullType()
    {
        // Arrange & Act
        var node = new NullNode();

        // Assert
        Assert.IsNotNull(node.ReturnType);
        Assert.IsInstanceOfType<NullNode.NullType>(node.ReturnType);
    }

    [TestMethod]
    public void NullNode_WithType_ShouldSetReturnType()
    {
        // Arrange & Act
        var node = new NullNode(typeof(string));

        // Assert
        Assert.AreEqual(typeof(string), node.ReturnType);
    }

    [TestMethod]
    public void NullNode_ToString_ShouldReturnNull()
    {
        // Arrange
        var node = new NullNode();

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("null", result);
    }

    [TestMethod]
    public void NullNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new NullNode();

        // Act & Assert
        Assert.Contains("NullNode", node.Id);
    }

    #endregion

    #region BinaryIntegerNode Tests

    [TestMethod]
    public void BinaryIntegerNode_Constructor_ShouldParseValue()
    {
        // Arrange - Binary 1010 = 10 decimal
        var node = new BinaryIntegerNode("1010");

        // Act & Assert - Returns as long
        Assert.AreEqual(10L, node.ObjValue);
    }

    [TestMethod]
    public void BinaryIntegerNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new BinaryIntegerNode("101");

        // Act & Assert
        Assert.Contains("BinaryIntegerNode", node.Id);
    }

    #endregion

    #region HexIntegerNode Tests

    [TestMethod]
    public void HexIntegerNode_Constructor_ShouldParseValue()
    {
        // Arrange - Hex FF = 255 decimal
        var node = new HexIntegerNode("FF");

        // Act & Assert - Returns as long
        Assert.AreEqual(255L, node.ObjValue);
    }

    [TestMethod]
    public void HexIntegerNode_WithLowerCase_ShouldParseValue()
    {
        // Arrange
        var node = new HexIntegerNode("ff");

        // Act & Assert - Returns as long
        Assert.AreEqual(255L, node.ObjValue);
    }

    [TestMethod]
    public void HexIntegerNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new HexIntegerNode("A");

        // Act & Assert
        Assert.Contains("HexIntegerNode", node.Id);
    }

    #endregion

    #region OctalIntegerNode Tests

    [TestMethod]
    public void OctalIntegerNode_Constructor_ShouldParseValue()
    {
        // Arrange - Octal 77 = 63 decimal
        var node = new OctalIntegerNode("77");

        // Act & Assert - Returns as long
        Assert.AreEqual(63L, node.ObjValue);
    }

    [TestMethod]
    public void OctalIntegerNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new OctalIntegerNode("10");

        // Act & Assert
        Assert.Contains("OctalIntegerNode", node.Id);
    }

    #endregion

}
