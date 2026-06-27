using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
    [TestMethod]
    public void CreateSetOperatorPositionIndexes_ValidInput_ReturnsCorrectIndexes()
    {
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


        var result = BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes(queryNode, keys);


        Assert.HasCount(2, result);
        Assert.AreEqual(0, result[0]);
        Assert.AreEqual(2, result[1]);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_PrimitiveType_ReturnsTrue()
    {
        var intType = typeof(int);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(intType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_StringType_ReturnsTrue()
    {
        var stringType = typeof(string);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(stringType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DecimalType_ReturnsTrue()
    {
        var decimalType = typeof(decimal);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(decimalType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DateTimeType_ReturnsTrue()
    {
        var dateTimeType = typeof(DateTime);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(dateTimeType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DateTimeOffsetType_ReturnsTrue()
    {
        var dateTimeOffsetType = typeof(DateTimeOffset);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(dateTimeOffsetType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_NullableDateTimeOffsetType_ReturnsTrue()
    {
        var nullableDateTimeOffsetType = typeof(DateTimeOffset?);


        var result =
            BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(nullableDateTimeOffsetType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_NullableIntType_ReturnsTrue()
    {
        var nullableIntType = typeof(int?);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(nullableIntType);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_ArrayType_ReturnsFalse()
    {
        var arrayType = typeof(int[]);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(arrayType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_ObjectType_ReturnsFalse()
    {
        var objectType = typeof(object);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(objectType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_ListType_ReturnsFalse()
    {
        var listType = typeof(List<string>);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(listType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_DictionaryType_ReturnsFalse()
    {
        var dictionaryType = typeof(Dictionary<string, string>);


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(dictionaryType);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeColumnInStarExpansion_NullType_ReturnsFalse()
    {
        Type? nullType = null;


        var result = BuildMetadataAndInferTypesVisitorUtilities.ShouldIncludeColumnInStarExpansion(nullType);


        Assert.IsFalse(result);
    }
}
