using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
    [TestMethod]
    public void FindClosestCommonParent_SameType_ReturnsSameType()
    {
        var stringType = typeof(string);


        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(stringType, stringType);


        Assert.AreEqual(stringType, result);
    }

    [TestMethod]
    public void FindClosestCommonParent_ParentChild_ReturnsParent()
    {
        var parentType = typeof(Exception);
        var childType = typeof(ArgumentException);


        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(parentType, childType);


        Assert.AreEqual(parentType, result);
    }

    [TestMethod]
    public void FindClosestCommonParent_ChildParent_ReturnsParent()
    {
        var parentType = typeof(Exception);
        var childType = typeof(ArgumentException);


        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(childType, parentType);


        Assert.AreEqual(parentType, result);
    }

    [TestMethod]
    public void FindClosestCommonParent_UnrelatedTypes_ReturnsObject()
    {
        var type1 = typeof(string);
        var type2 = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.FindClosestCommonParent(type1, type2);


        Assert.AreEqual(typeof(object), result);
    }

    [TestMethod]
    public void MakeTypeNullable_ValueType_ReturnsNullable()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(intType);


        Assert.AreEqual(typeof(int?), result);
    }

    [TestMethod]
    public void MakeTypeNullable_ReferenceType_ReturnsSameType()
    {
        var stringType = typeof(string);


        var result = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(stringType);


        Assert.AreEqual(stringType, result);
    }

    [TestMethod]
    public void MakeTypeNullable_AlreadyNullable_ReturnsSameType()
    {
        var nullableIntType = typeof(int?);


        var result = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(nullableIntType);


        Assert.AreEqual(nullableIntType, result);
    }

    [TestMethod]
    public void MakeTypeNullable_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(null!));
    }

    [TestMethod]
    public void StripNullable_NullableType_ReturnsUnderlyingType()
    {
        var nullableIntType = typeof(int?);


        var result = BuildMetadataAndInferTypesVisitorUtilities.StripNullable(nullableIntType);


        Assert.AreEqual(typeof(int), result);
    }

    [TestMethod]
    public void StripNullable_NonNullableType_ReturnsSameType()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.StripNullable(intType);


        Assert.AreEqual(intType, result);
    }

    [TestMethod]
    public void StripNullable_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BuildMetadataAndInferTypesVisitorUtilities.StripNullable(null!));
    }
}
