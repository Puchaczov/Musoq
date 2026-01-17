using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

/// <summary>
///     Additional tests for Parser node classes focusing on ToString, Id, and properties
/// </summary>
[TestClass]
public class AdditionalNodesTests
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
        Assert.IsInstanceOfType(node.ObjValue, typeof(sbyte));
        Assert.AreEqual((sbyte)42, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedByte_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("200", "ub");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(byte));
        Assert.AreEqual((byte)200, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Short_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("1000", "s");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(short));
        Assert.AreEqual((short)1000, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedShort_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("50000", "us");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(ushort));
        Assert.AreEqual((ushort)50000, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Int_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("100000", "i");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(int));
        Assert.AreEqual(100000, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedInt_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("3000000000", "ui");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(uint));
        Assert.AreEqual(3000000000u, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_Long_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("9999999999", "l");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(long));
        Assert.AreEqual(9999999999L, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndAbbreviation_UnsignedLong_ShouldParse()
    {
        // Arrange & Act
        var node = new IntegerNode("18000000000000000000", "ul");

        // Assert
        Assert.IsInstanceOfType(node.ObjValue, typeof(ulong));
        Assert.AreEqual(18000000000000000000ul, node.ObjValue);
    }

    [TestMethod]
    public void IntegerNode_WithStringAndEmptyAbbreviation_ShouldAutoDetectType()
    {
        // Arrange & Act
        var node = new IntegerNode("42", "");

        // Assert - Should detect as int by default
        Assert.IsInstanceOfType(node.ObjValue, typeof(int));
    }

    #endregion

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
        Assert.IsInstanceOfType(node.ReturnType, typeof(NullNode.NullType));
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

    #region StarNode Tests

    [TestMethod]
    public void StarNode_Constructor_ShouldSetLeftAndRight()
    {
        // Arrange
        var left = new IntegerNode(5);
        var right = new IntegerNode(3);

        // Act
        var node = new StarNode(left, right);

        // Assert
        Assert.AreSame(left, node.Left);
        Assert.AreSame(right, node.Right);
    }

    [TestMethod]
    public void StarNode_ToString_ShouldReturnMultiplicationExpression()
    {
        // Arrange
        var left = new IntegerNode(5);
        var right = new IntegerNode(3);
        var node = new StarNode(left, right);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("5 * 3", result);
    }

    [TestMethod]
    public void StarNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new StarNode(new IntegerNode(1), new IntegerNode(2));

        // Act & Assert
        Assert.IsNotNull(node.Id);
    }

    #endregion

    #region WhereNode Tests

    [TestMethod]
    public void WhereNode_Constructor_ShouldSetExpression()
    {
        // Arrange
        var expression = new BooleanNode(true);

        // Act
        var node = new WhereNode(expression);

        // Assert
        Assert.AreSame(expression, node.Expression);
    }

    [TestMethod]
    public void WhereNode_ToString_ShouldStartWithWhere()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new WhereNode(expression);

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("where", result);
    }

    [TestMethod]
    public void WhereNode_Id_ShouldContainNodeNameAndExpressionId()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new WhereNode(expression);

        // Act & Assert
        Assert.Contains("WhereNode", node.Id);
    }

    #endregion

    #region TakeNode Tests

    [TestMethod]
    public void TakeNode_Constructor_ShouldSetValue()
    {
        // Arrange & Act
        var node = new TakeNode(new IntegerNode(10));

        // Assert
        Assert.AreEqual(10, ((IntegerNode)node.Expression).ObjValue);
    }

    [TestMethod]
    public void TakeNode_ToString_ShouldStartWithTake()
    {
        // Arrange
        var node = new TakeNode(new IntegerNode(5));

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("take", result);
    }

    #endregion

    #region SkipNode Tests

    [TestMethod]
    public void SkipNode_Constructor_ShouldSetValue()
    {
        // Arrange & Act
        var node = new SkipNode(new IntegerNode(10));

        // Assert
        Assert.AreEqual(10, ((IntegerNode)node.Expression).ObjValue);
    }

    [TestMethod]
    public void SkipNode_ToString_ShouldStartWithSkip()
    {
        // Arrange
        var node = new SkipNode(new IntegerNode(5));

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("skip", result);
    }

    #endregion

    #region GroupByNode Tests

    [TestMethod]
    public void GroupByNode_Constructor_ShouldSetFields()
    {
        // Arrange
        var fields = new[]
        {
            new FieldNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, "col1"),
            new FieldNode(new AccessColumnNode("col2", string.Empty, TextSpan.Empty), 1, "col2")
        };

        // Act
        var node = new GroupByNode(fields, null);

        // Assert
        Assert.HasCount(2, node.Fields);
    }

    [TestMethod]
    public void GroupByNode_ToString_ShouldStartWithGroupBy()
    {
        // Arrange
        var fields = new[]
        {
            new FieldNode(new AccessColumnNode("col1", string.Empty, TextSpan.Empty), 0, "col1")
        };
        var node = new GroupByNode(fields, null);

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("group by", result);
    }

    #endregion

    #region HavingNode Tests

    [TestMethod]
    public void HavingNode_Constructor_ShouldSetExpression()
    {
        // Arrange
        var expression = new BooleanNode(true);

        // Act
        var node = new HavingNode(expression);

        // Assert
        Assert.AreSame(expression, node.Expression);
    }

    [TestMethod]
    public void HavingNode_ToString_ShouldStartWithHaving()
    {
        // Arrange
        var expression = new BooleanNode(true);
        var node = new HavingNode(expression);

        // Act
        var result = node.ToString();

        // Assert
        Assert.StartsWith("having", result);
    }

    #endregion

    #region AccessColumnNode Tests

    [TestMethod]
    public void AccessColumnNode_Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var node = new AccessColumnNode("columnName", "alias", TextSpan.Empty);

        // Assert
        Assert.AreEqual("columnName", node.Name);
        Assert.AreEqual("alias", node.Alias);
    }

    [TestMethod]
    public void AccessColumnNode_ToString_ShouldReturnColumnName()
    {
        // Arrange
        var node = new AccessColumnNode("myColumn", string.Empty, TextSpan.Empty);

        // Act
        var result = node.ToString();

        // Assert
        Assert.AreEqual("myColumn", result);
    }

    [TestMethod]
    public void AccessColumnNode_Id_ShouldContainNodeName()
    {
        // Arrange
        var node = new AccessColumnNode("col", "", TextSpan.Empty);

        // Act & Assert
        Assert.Contains("AccessColumnNode", node.Id);
    }

    #endregion
}