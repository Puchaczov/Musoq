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

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_PrimitiveType_ReturnsTrue()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(intType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_StringType_ReturnsTrue()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(stringType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DecimalType_ReturnsTrue()
    {
        // Arrange
        var decimalType = typeof(decimal);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(decimalType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DateTimeType_ReturnsTrue()
    {
        // Arrange
        var dateTimeType = typeof(DateTime);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(dateTimeType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DateTimeOffsetType_ReturnsTrue()
    {
        // Arrange
        var dateTimeOffsetType = typeof(DateTimeOffset);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(dateTimeOffsetType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_NullableDateTimeOffsetType_ReturnsTrue()
    {
        // Arrange
        var nullableDateTimeOffsetType = typeof(DateTimeOffset?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(nullableDateTimeOffsetType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_NullableIntType_ReturnsTrue()
    {
        // Arrange
        var nullableIntType = typeof(int?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(nullableIntType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_ArrayType_ReturnsFalse()
    {
        // Arrange
        var arrayType = typeof(int[]);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(arrayType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_ObjectType_ReturnsFalse()
    {
        // Arrange
        var objectType = typeof(object);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(objectType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_ListType_ReturnsFalse()
    {
        // Arrange
        var listType = typeof(List<string>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(listType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DictionaryType_ReturnsFalse()
    {
        // Arrange
        var dictionaryType = typeof(Dictionary<string, string>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(dictionaryType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_NullType_ReturnsFalse()
    {
        // Arrange
        Type nullType = null;
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(nullType);
        
        // Assert
        Assert.IsFalse(result);
    }

    #region IsValidQueryExpressionType Tests

    [TestMethod]
    public void IsValidQueryExpressionType_NullParameter_ReturnsFalse()
    {
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(null);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_SqlNullType_ReturnsTrue()
    {
        // Arrange - Get the NullType from NullNode
        var nullNode = new Musoq.Parser.Nodes.NullNode();
        var nullType = nullNode.ReturnType;
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(nullType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_ArrayType_ReturnsFalse()
    {
        // Arrange
        var arrayType = typeof(int[]);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(arrayType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_StringArrayType_ReturnsFalse()
    {
        // Arrange
        var arrayType = typeof(string[]);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(arrayType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_IntType_ReturnsTrue()
    {
        // Arrange
        var intType = typeof(int);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(intType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_NullableIntType_ReturnsTrue()
    {
        // Arrange
        var nullableIntType = typeof(int?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(nullableIntType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_StringType_ReturnsTrue()
    {
        // Arrange
        var stringType = typeof(string);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(stringType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_DecimalType_ReturnsTrue()
    {
        // Arrange
        var decimalType = typeof(decimal);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(decimalType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_NullableDecimalType_ReturnsTrue()
    {
        // Arrange
        var nullableDecimalType = typeof(decimal?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(nullableDecimalType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_DateTimeType_ReturnsTrue()
    {
        // Arrange
        var dateTimeType = typeof(DateTime);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(dateTimeType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_NullableDateTimeType_ReturnsTrue()
    {
        // Arrange
        var nullableDateTimeType = typeof(DateTime?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(nullableDateTimeType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_DateTimeOffsetType_ReturnsTrue()
    {
        // Arrange
        var dateTimeOffsetType = typeof(DateTimeOffset);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(dateTimeOffsetType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_GuidType_ReturnsTrue()
    {
        // Arrange
        var guidType = typeof(Guid);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(guidType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_NullableGuidType_ReturnsTrue()
    {
        // Arrange
        var nullableGuidType = typeof(Guid?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(nullableGuidType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_TimeSpanType_ReturnsTrue()
    {
        // Arrange
        var timeSpanType = typeof(TimeSpan);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(timeSpanType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_NullableTimeSpanType_ReturnsTrue()
    {
        // Arrange
        var nullableTimeSpanType = typeof(TimeSpan?);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(nullableTimeSpanType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_BoolType_ReturnsTrue()
    {
        // Arrange
        var boolType = typeof(bool);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(boolType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_CharType_ReturnsTrue()
    {
        // Arrange
        var charType = typeof(char);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(charType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_DoubleType_ReturnsTrue()
    {
        // Arrange
        var doubleType = typeof(double);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(doubleType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_FloatType_ReturnsTrue()
    {
        // Arrange
        var floatType = typeof(float);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(floatType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_LongType_ReturnsTrue()
    {
        // Arrange
        var longType = typeof(long);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(longType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_ByteType_ReturnsTrue()
    {
        // Arrange
        var byteType = typeof(byte);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(byteType);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_ObjectType_ReturnsFalse()
    {
        // Arrange
        var objectType = typeof(object);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(objectType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_ListType_ReturnsFalse()
    {
        // Arrange
        var listType = typeof(List<string>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(listType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_DictionaryType_ReturnsFalse()
    {
        // Arrange
        var dictionaryType = typeof(Dictionary<string, string>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(dictionaryType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_CustomClassType_ReturnsFalse()
    {
        // Arrange
        var customClassType = typeof(Exception);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(customClassType);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsValidQueryExpressionType_StructType_ReturnsFalse()
    {
        // Arrange - KeyValuePair is a struct
        var structType = typeof(KeyValuePair<string, int>);
        
        // Act
        var result = BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(structType);
        
        // Assert
        Assert.IsFalse(result);
    }

    #endregion
}
