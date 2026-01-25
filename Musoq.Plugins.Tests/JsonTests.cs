using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Helpers;

namespace Musoq.Plugins.Tests;

[TestClass]
public class JsonTests : LibraryBaseBaseTests
{
    private static readonly int[] Obj = [1, 2, 3];

    [TestMethod]
    public void WhenSerializingPrimitiveType_ShouldPass()
    {
        Assert.AreEqual("1", Library.ToJson(1));
    }

    [TestMethod]
    public void WhenSerializingObject_ShouldPass()
    {
        Assert.AreEqual("{\"Name\":\"John\",\"Age\":30}", Library.ToJson(new { Name = "John", Age = 30 }));
    }

    [TestMethod]
    public void WhenSerializingArray_ShouldPass()
    {
        Assert.AreEqual("[1,2,3]", Library.ToJson(Obj));
    }

    [TestMethod]
    public void WhenNull_ShouldPass()
    {
        Assert.IsNull(Library.ToJson<int?>(null));
    }

    [TestMethod]
    public void WhenJsonIsInvalid_ReturnsEmptyArray()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(invalidJson, "$.test");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void WhenExtractingSimpleProperty_ReturnsCorrectValue()
    {
        // Arrange
        var json = @"{ ""test"": ""value"" }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$.test");

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("value", result[0]);
    }

    [TestMethod]
    public void WhenExtractingNestedProperty_ReturnsCorrectValue()
    {
        // Arrange
        var json = @"{ ""abc"": { ""test"": ""something"" } }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$.abc.test");

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("something", result[0]);
    }

    [TestMethod]
    public void WhenExtractingFromArray_ReturnsAllValues()
    {
        // Arrange
        var json = @"[{ ""abc"": { ""test"": ""something"" } }, { ""abc"": { ""test"": ""something2"" } }]";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$[*].abc.test");

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual("something", result[0]);
        Assert.AreEqual("something2", result[1]);
    }

    [TestMethod]
    public void WhenExtractingSpecificArrayIndex_ReturnsCorrectValue()
    {
        // Arrange
        var json = @"[{ ""value"": ""first"" }, { ""value"": ""second"" }]";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$[1].value");

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("second", result[0]);
    }

    [TestMethod]
    public void WhenUsingWildcardWithObjects_ReturnsAllMatchingValues()
    {
        // Arrange
        var json = @"{ ""item1"": { ""value"": ""one"" }, ""item2"": { ""value"": ""two"" } }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$.*.value");

        // Assert
        Assert.HasCount(2, result);
        CollectionAssert.Contains(result, "one");
        CollectionAssert.Contains(result, "two");
    }

    [TestMethod]
    public void WhenPathNotFound_ReturnsEmptyArray()
    {
        // Arrange
        var json = @"{ ""test"": ""value"" }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$.nonexistent.path");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void WhenExtractingWithoutDollarSign_ReturnsCorrectValue()
    {
        // Arrange
        var json = @"{ ""abc"": { ""test"": ""something"" } }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "abc.test");

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("something", result[0]);
    }

    [TestMethod]
    public void WhenExtractingNonStringValue_ReturnsStringRepresentation()
    {
        // Arrange
        var json = @"{ ""number"": 42, ""boolean"": true }";

        // Act
        var numberResult = JsonExtractorHelper.ExtractFromJson(json, "$.number");
        var boolResult = JsonExtractorHelper.ExtractFromJson(json, "$.boolean");

        // Assert
        Assert.HasCount(1, numberResult);
        Assert.AreEqual("42", numberResult[0]);
        Assert.HasCount(1, boolResult);
        Assert.AreEqual("true", boolResult[0]);
    }

    [TestMethod]
    public void WhenExtractingFromNestedArrays_ReturnsAllValues()
    {
        // Arrange
        var json = @"{ ""items"": [{ ""values"": [{ ""test"": ""first"" }, { ""test"": ""second"" }] }] }";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$.items[*].values[*].test");

        // Assert
        Assert.HasCount(2, result);
        CollectionAssert.Contains(result, "first");
        CollectionAssert.Contains(result, "second");
    }

    [TestMethod]
    public void WhenArrayIndexOutOfBounds_ReturnsEmptyArray()
    {
        // Arrange
        var json = @"[{ ""value"": ""first"" }]";

        // Act
        var result = JsonExtractorHelper.ExtractFromJson(json, "$[1].value");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void WhenExtractingFromComplexArray_ReturnsAllValues()
    {
        // Arrange
        var json = @"[
        { ""abc"": { ""test"": ""something"", ""value"": 42 } },
        { ""abc"": { ""test"": ""something2"", ""value"": 43 } },
        { ""abc"": { ""test"": ""something3"", ""value"": 44 } }
    ]";

        // Act
        var testResults = JsonExtractorHelper.ExtractFromJson(json, "$[*].abc.test");
        var valueResults = JsonExtractorHelper.ExtractFromJson(json, "$[*].abc.value");

        // Assert
        Assert.HasCount(3, testResults);
        Assert.AreEqual("something", testResults[0]);
        Assert.AreEqual("something2", testResults[1]);
        Assert.AreEqual("something3", testResults[2]);

        Assert.HasCount(3, valueResults);
        Assert.AreEqual("42", valueResults[0]);
        Assert.AreEqual("43", valueResults[1]);
        Assert.AreEqual("44", valueResults[2]);
    }

    [TestMethod]
    public void WhenExtractingDifferentDataTypes_ReturnsCorrectStringRepresentations()
    {
        // Arrange
        var json = @"{
        ""string"": ""text"",
        ""number"": 42,
        ""decimal"": 42.5,
        ""boolean"": true,
        ""nullValue"": null,
        ""object"": { ""nested"": ""value"" },
        ""array"": [1, 2, 3]
    }";

        // Act & Assert
        var stringResult = JsonExtractorHelper.ExtractFromJson(json, "$.string");
        Assert.AreEqual("text", stringResult[0]);

        var numberResult = JsonExtractorHelper.ExtractFromJson(json, "$.number");
        Assert.AreEqual("42", numberResult[0]);

        var decimalResult = JsonExtractorHelper.ExtractFromJson(json, "$.decimal");
        Assert.AreEqual("42.5", decimalResult[0]);

        var booleanResult = JsonExtractorHelper.ExtractFromJson(json, "$.boolean");
        Assert.AreEqual("true", booleanResult[0]);

        var nullResult = JsonExtractorHelper.ExtractFromJson(json, "$.nullValue");
        Assert.IsEmpty(nullResult);

        // Testing object extraction
        var objectResult = JsonExtractorHelper.ExtractFromJson(json, "$.object.nested");
        Assert.HasCount(1, objectResult);
        Assert.AreEqual("value", objectResult[0]);

        // Testing array extraction
        var arrayResult = JsonExtractorHelper.ExtractFromJson(json, "$.array[*]");
        Assert.HasCount(3, arrayResult);
        Assert.AreEqual("1", arrayResult[0]);
        Assert.AreEqual("2", arrayResult[1]);
        Assert.AreEqual("3", arrayResult[2]);

        // Testing direct object/array extraction (should return JSON string representation)
        var fullObjectResult = JsonExtractorHelper.ExtractFromJson(json, "$.object");
        Assert.HasCount(1, fullObjectResult);
        Assert.AreEqual("{\"nested\":\"value\"}", fullObjectResult[0]);

        var fullArrayResult = JsonExtractorHelper.ExtractFromJson(json, "$.array");
        Assert.HasCount(1, fullArrayResult);
        Assert.AreEqual("[1,2,3]", fullArrayResult[0]);
    }

    [TestMethod]
    public void WhenUsingDifferentArrayNotations_ReturnsCorrectValues()
    {
        // Arrange
        var json = @"{
        ""items"": [
            { ""id"": 1, ""values"": [""a"", ""b"", ""c""] },
            { ""id"": 2, ""values"": [""d"", ""e"", ""f""] }
        ]
    }";

        // Act
        var specificArrayResult = JsonExtractorHelper.ExtractFromJson(json, "$.items[0].values[1]");
        var wildCardArrayResult = JsonExtractorHelper.ExtractFromJson(json, "$.items[*].values[0]");
        var doubleWildcardResult = JsonExtractorHelper.ExtractFromJson(json, "$.items[*].values[*]");

        // Assert
        Assert.HasCount(1, specificArrayResult);
        Assert.AreEqual("b", specificArrayResult[0]);

        Assert.HasCount(2, wildCardArrayResult);
        Assert.AreEqual("a", wildCardArrayResult[0]);
        Assert.AreEqual("d", wildCardArrayResult[1]);

        Assert.HasCount(6, doubleWildcardResult);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "e", "f" }, doubleWildcardResult);
    }

    [TestMethod]
    public void WhenHandlingNestedArraysWithMixedTypes_ReturnsCorrectValues()
    {
        // Arrange
        var json = @"{
        ""data"": [
            {
                ""items"": [
                    { ""value"": true },
                    { ""value"": 42 },
                    { ""value"": ""text"" }
                ]
            },
            {
                ""items"": [
                    { ""value"": false },
                    { ""value"": 43 },
                    { ""value"": ""another"" }
                ]
            }
        ]
    }";

        // Act
        var results = JsonExtractorHelper.ExtractFromJson(json, "$.data[*].items[*].value");

        // Assert
        Assert.HasCount(6, results);
        Assert.AreEqual("true", results[0]);
        Assert.AreEqual("42", results[1]);
        Assert.AreEqual("text", results[2]);
        Assert.AreEqual("false", results[3]);
        Assert.AreEqual("43", results[4]);
        Assert.AreEqual("another", results[5]);
    }

    [TestMethod]
    public void WhenHandlingEmptyArraysAndObjects_ReturnsEmptyArray()
    {
        // Arrange
        var json = @"{
        ""emptyArray"": [],
        ""emptyObject"": {},
        ""arrayWithEmpty"": [
            { },
            { ""value"": ""something"" }
        ]
    }";

        // Act
        var emptyArrayResult = JsonExtractorHelper.ExtractFromJson(json, "$.emptyArray[*]");
        var emptyObjectResult = JsonExtractorHelper.ExtractFromJson(json, "$.emptyObject.value");
        var arrayWithEmptyResult = JsonExtractorHelper.ExtractFromJson(json, "$.arrayWithEmpty[*].value");

        // Assert
        Assert.IsEmpty(emptyArrayResult);
        Assert.IsEmpty(emptyObjectResult);
        Assert.HasCount(1, arrayWithEmptyResult);
        Assert.AreEqual("something", arrayWithEmptyResult[0]);
    }

    [TestMethod]
    public void WhenHandlingOutOfBoundsAndInvalidPaths_ReturnsEmptyArray()
    {
        // Arrange
        var json = @"{
        ""array"": [1, 2, 3],
        ""nested"": { ""value"": ""test"" }
    }";

        // Act & Assert
        var outOfBoundsResult = JsonExtractorHelper.ExtractFromJson(json, "$.array[5]");
        Assert.IsEmpty(outOfBoundsResult);

        var invalidPathResult = JsonExtractorHelper.ExtractFromJson(json, "$.nonexistent.path");
        Assert.IsEmpty(invalidPathResult);

        var invalidArrayPathResult = JsonExtractorHelper.ExtractFromJson(json, "$.nested[0]");
        Assert.IsEmpty(invalidArrayPathResult);
    }

    [TestMethod]
    public void WhenPathContainsExtraDotsAndBrackets_HandlesGracefully()
    {
        // Arrange
        var json = @"{ ""value"": ""test"" }";

        // Act & Assert
        var extraDotsResult = JsonExtractorHelper.ExtractFromJson(json, "$...value");
        Assert.HasCount(1, extraDotsResult);
        Assert.AreEqual("test", extraDotsResult[0]);

        var extraBracketsResult = JsonExtractorHelper.ExtractFromJson(json, "$.[value]");
        Assert.HasCount(1, extraBracketsResult);
        Assert.AreEqual("test", extraBracketsResult[0]);
    }

    [TestMethod]
    public void WhenHandlingSpecialCharactersInPaths_ReturnsCorrectValues()
    {
        // Arrange
        var json = @"{
        ""special.key"": ""value1"",
        ""special[key]"": ""value2"",
        ""nested"": {
            ""special.key"": ""value3"",
            ""array[0]"": ""value4""
        }
    }";

        // Act & Assert
        // Test different quote styles
        var result1 = JsonExtractorHelper.ExtractFromJson(json, "$['special.key']");
        Assert.AreEqual("value1", result1[0]);

        var result2 = JsonExtractorHelper.ExtractFromJson(json, "$[\"special.key\"]");
        Assert.AreEqual("value1", result2[0]);

        // Test nested paths with special characters
        var result3 = JsonExtractorHelper.ExtractFromJson(json, "$.nested['special.key']");
        Assert.AreEqual("value3", result3[0]);

        var result4 = JsonExtractorHelper.ExtractFromJson(json, "$['nested']['special.key']");
        Assert.AreEqual("value3", result4[0]);

        // Test brackets in property names
        var result5 = JsonExtractorHelper.ExtractFromJson(json, "$['special[key]']");
        Assert.AreEqual("value2", result5[0]); //throws exception

        var result6 = JsonExtractorHelper.ExtractFromJson(json, "$.nested['array[0]']");
        Assert.AreEqual("value4", result6[0]);
    }

    [TestMethod]
    public void WhenHandlingComplexPropertyNames_ReturnsCorrectValues()
    {
        // Arrange
        var json = @"{
        ""normal"": ""value0"",
        ""special.key"": ""value1"",
        ""special[key]"": ""value2"",
        ""special[key].more"": ""value3"",
        ""nested"": {
            ""special.key"": ""value4"",
            ""array[0]"": ""value5"",
            ""complex[name].with.dot"": ""value6""
        }
    }";

        // Act & Assert
        // Test various property name formats
        var result1 = JsonExtractorHelper.ExtractFromJson(json, "$['special[key]']");
        Assert.AreEqual("value2", result1[0]);

        var result2 = JsonExtractorHelper.ExtractFromJson(json, "$['special[key].more']");
        Assert.AreEqual("value3", result2[0]);

        var result3 = JsonExtractorHelper.ExtractFromJson(json, "$.nested['complex[name].with.dot']");
        Assert.AreEqual("value6", result3[0]);

        // Mix of normal and special properties
        var result4 = JsonExtractorHelper.ExtractFromJson(json, "$['nested']['array[0]']");
        Assert.AreEqual("value5", result4[0]);

        // Normal property access
        var result5 = JsonExtractorHelper.ExtractFromJson(json, "$.normal");
        Assert.AreEqual("value0", result5[0]);
    }

    [TestMethod]
    public void WhenHandlingVeryComplexPaths_ReturnsCorrectValues()
    {
        // Arrange
        var json = @"{
        ""simple"": ""value1"",
        ""with.dot"": ""value2"",
        ""with[bracket]"": ""value3"",
        ""nested"": {
            ""complex[name].with.dot"": ""value4"",
            ""array"": [
                { ""special.name[0]"": ""value5"" }
            ]
        }
    }";

        // Act & Assert
        // Simple path
        var result1 = JsonExtractorHelper.ExtractFromJson(json, "$.simple");
        Assert.AreEqual("value1", result1[0]);

        // Path with dot
        var result2 = JsonExtractorHelper.ExtractFromJson(json, "$['with.dot']");
        Assert.AreEqual("value2", result2[0]);

        // Path with bracket
        var result3 = JsonExtractorHelper.ExtractFromJson(json, "$['with[bracket]']");
        Assert.AreEqual("value3", result3[0]);

        // Complex nested path
        var result4 = JsonExtractorHelper.ExtractFromJson(json, "$.nested['complex[name].with.dot']");
        Assert.AreEqual("value4", result4[0]);

        // Array with special property name
        var result5 = JsonExtractorHelper.ExtractFromJson(json, "$.nested.array[0]['special.name[0]']");
        Assert.AreEqual("value5", result5[0]);

        // Alternative notation
        var result6 = JsonExtractorHelper.ExtractFromJson(json, "$['nested']['complex[name].with.dot']");
        Assert.AreEqual("value4", result6[0]);
    }
}
