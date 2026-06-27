using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
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
        var nullNode = new NullNode();
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
