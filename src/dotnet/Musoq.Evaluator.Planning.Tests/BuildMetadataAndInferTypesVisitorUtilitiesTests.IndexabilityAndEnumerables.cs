using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
    [TestMethod]
    public void HasIndexer_ArrayType_ReturnsFalse()
    {
        var arrayType = typeof(int[]);


        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(arrayType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasIndexer_StringType_ReturnsTrue()
    {
        var stringType = typeof(string);


        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(stringType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasIndexer_ListType_ReturnsTrue()
    {
        var listType = typeof(List<int>);


        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(listType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasIndexer_NonIndexableType_ReturnsFalse()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(intType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasIndexer_NullType_ReturnsFalse()
    {
        var result = BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(null);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsIndexableType_ArrayType_ReturnsTrue()
    {
        var arrayType = typeof(int[]);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(arrayType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsIndexableType_StringType_ReturnsTrue()
    {
        var stringType = typeof(string);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(stringType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsIndexableType_NonIndexableType_ReturnsFalse()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(intType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsPrimitiveType_IntType_ReturnsTrue()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(intType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_StringType_ReturnsTrue()
    {
        var stringType = typeof(string);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(stringType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_DecimalType_ReturnsTrue()
    {
        var decimalType = typeof(decimal);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(decimalType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_DateTimeType_ReturnsTrue()
    {
        var dateTimeType = typeof(DateTime);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(dateTimeType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPrimitiveType_ComplexType_ReturnsFalse()
    {
        var complexType = typeof(List<int>);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsPrimitiveType(complexType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsGenericEnumerable_ListType_ReturnsTrueWithElementType()
    {
        var listType = typeof(List<string>);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsGenericEnumerable(listType, out var elementType);


        Assert.IsTrue(result);
        Assert.AreEqual(typeof(string), elementType);
    }

    [TestMethod]
    public void IsGenericEnumerable_NonGenericType_ReturnsFalse()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsGenericEnumerable(intType, out var elementType);


        Assert.IsFalse(result);
        Assert.IsNull(elementType);
    }

    [TestMethod]
    public void IsArray_ArrayType_ReturnsTrueWithElementType()
    {
        var arrayType = typeof(string[]);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsArray(arrayType, out var elementType);


        Assert.IsTrue(result);
        Assert.AreEqual(typeof(string), elementType);
    }

    [TestMethod]
    public void IsArray_NonArrayType_ReturnsFalse()
    {
        var listType = typeof(List<string>);


        var result = BuildMetadataAndInferTypesVisitorUtilities.IsArray(listType, out var elementType);


        Assert.IsFalse(result);
        Assert.IsNull(elementType);
    }
}
