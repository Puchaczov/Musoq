using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
    [TestMethod]
    public void FindClosestCommonParent_SameType_ReturnsSameType()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(stringType, stringType);
        
        // Assert
        Assert.AreEqual(stringType, result);
    }

    [TestMethod]
    public void FindClosestCommonParent_ParentChild_ReturnsParent()
    {
        // Arrange
        var parentType = typeof(Exception);
        var childType = typeof(ArgumentException);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(parentType, childType);
        
        // Assert
        Assert.AreEqual(parentType, result);
    }

    [TestMethod]
    public void FindClosestCommonParent_ChildParent_ReturnsParent()
    {
        // Arrange
        var parentType = typeof(Exception);
        var childType = typeof(ArgumentException);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(childType, parentType);
        
        // Assert
        Assert.AreEqual(parentType, result);
    }

    [TestMethod]
    public void FindClosestCommonParent_UnrelatedTypes_ReturnsObject()
    {
        // Arrange
        var type1 = typeof(string);
        var type2 = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(type1, type2);
        
        // Assert
        Assert.AreEqual(typeof(object), result);
    }

    [TestMethod]
    public void MakeTypeNullable_ValueType_ReturnsNullable()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(intType);
        
        // Assert
        Assert.AreEqual(typeof(int?), result);
    }

    [TestMethod]
    public void MakeTypeNullable_ReferenceType_ReturnsSameType()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(stringType);
        
        // Assert
        Assert.AreEqual(stringType, result);
    }

    [TestMethod]
    public void MakeTypeNullable_AlreadyNullable_ReturnsSameType()
    {
        // Arrange
        var nullableIntType = typeof(int?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(nullableIntType);
        
        // Assert
        Assert.AreEqual(nullableIntType, result);
    }

    [TestMethod]
    public void MakeTypeNullable_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(null));
    }

    [TestMethod]
    public void StripNullable_NullableType_ReturnsUnderlyingType()
    {
        // Arrange
        var nullableIntType = typeof(int?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.StripNullable(nullableIntType);
        
        // Assert
        Assert.AreEqual(typeof(int), result);
    }

    [TestMethod]
    public void StripNullable_NonNullableType_ReturnsSameType()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.StripNullable(intType);
        
        // Assert
        Assert.AreEqual(intType, result);
    }

    [TestMethod]
    public void StripNullable_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BuildMetadataAndInferTypesVisitorUtilities.StripNullable(null));
    }

    [TestMethod]
    public void HasIndexer_ArrayType_ReturnsFalse()
    {
        // Arrange - Arrays don't have indexer properties, they have special array access
        var arrayType = typeof(int[]);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(arrayType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasIndexer_StringType_ReturnsTrue()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(stringType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasIndexer_ListType_ReturnsTrue()
    {
        // Arrange
        var listType = typeof(List<int>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(listType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasIndexer_NonIndexableType_ReturnsFalse()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(intType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasIndexer_NullType_ReturnsFalse()
    {
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(null);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsIndexableType_ArrayType_ReturnsTrue()
    {
        // Arrange
        var arrayType = typeof(int[]);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(arrayType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsIndexableType_StringType_ReturnsTrue()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(stringType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsIndexableType_NonIndexableType_ReturnsFalse()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(intType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrimitiveType_IntType_ReturnsTrue()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(intType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_StringType_ReturnsTrue()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(stringType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_DecimalType_ReturnsTrue()
    {
        // Arrange
        var decimalType = typeof(decimal);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(decimalType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_DateTimeType_ReturnsTrue()
    {
        // Arrange
        var dateTimeType = typeof(DateTime);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(dateTimeType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_ComplexType_ReturnsFalse()
    {
        // Arrange
        var complexType = typeof(List<int>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(complexType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsGenericEnumerable_ListType_ReturnsTrueWithElementType()
    {
        // Arrange
        var listType = typeof(List<string>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsGenericEnumerable(listType, out var elementType);
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(string), elementType);
    }

    [TestMethod]
    public void IsGenericEnumerable_NonGenericType_ReturnsFalse()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsGenericEnumerable(intType, out var elementType);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(elementType);
    }

    [TestMethod]
    public void IsArray_ArrayType_ReturnsTrueWithElementType()
    {
        // Arrange
        var arrayType = typeof(string[]);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsArray(arrayType, out var elementType);
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(string), elementType);
    }

    [TestMethod]
    public void IsArray_NonArrayType_ReturnsFalse()
    {
        // Arrange
        var listType = typeof(List<string>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsArray(listType, out var elementType);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(elementType);
    }

    [TestMethod]
    public void CreateSetOperatorPositionIndexes_ValidInput_ReturnsCorrectIndexes()
    {
        // Arrange
        var fields = new[]
        {
            new FieldNode(new IntegerNode("1", "s"), 0, "Field1"),
            new FieldNode(new IntegerNode("2", "s"), 1, "Field2"),
            new FieldNode(new IntegerNode("3", "s"), 2, "Field3")
        };
        var selectNode = new SelectNode(fields);
        var fromNode = new SchemaFromNode("test", "table", new ArgsListNode([]), "t1", typeof(object), 1);
        var queryNode = new QueryNode(selectNode, fromNode, null, null, null, null, null);
        var keys = new[] { "Field1", "Field3" };
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes(queryNode, keys);
        
        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual(0, result[0]); // Field1 is at index 0
        Assert.AreEqual(2, result[1]); // Field3 is at index 2
    }
}
