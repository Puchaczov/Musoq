using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

public partial class SafeDataAccessTests
{
    #region SafeArrayAccess - Array Tests

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NullArray_ReturnsDefault()
    {
        // Arrange
        int[]? array = null;

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_EmptyArray_ReturnsDefault()
    {
        // Arrange
        var array = Array.Empty<int>();

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_ValidIndex_ReturnsElement()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 2);

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, -1);

        // Assert
        Assert.AreEqual(5, result); // Last element
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NegativeIndex_SecondFromEnd()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, -2);

        // Assert
        Assert.AreEqual(4, result); // Second to last
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_IndexOutOfBounds_ReturnsDefault()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 10);

        // Assert
        Assert.AreEqual(default, result);
    }

    #endregion

    #region SafeArrayAccess - String Tests

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NullString_ReturnsNullChar()
    {
        // Arrange
        string? str = null;

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 0);

        // Assert
        Assert.AreEqual('\0', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_EmptyString_ReturnsNullChar()
    {
        // Arrange
        var str = "";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 0);

        // Assert
        Assert.AreEqual('\0', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_ValidIndex_ReturnsChar()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 1);

        // Assert
        Assert.AreEqual('e', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, -1);

        // Assert
        Assert.AreEqual('o', result); // Last character
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_OutOfBounds_ReturnsNullChar()
    {
        // Arrange
        var str = "Hi";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 10);

        // Assert
        Assert.AreEqual('\0', result);
    }

    #endregion

    #region SafeArrayAccess - Dictionary Tests

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullDictionary_ReturnsDefault()
    {
        // Arrange
        Dictionary<string, int>? dict = null;

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, "key");

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullKey_ReturnsDefault()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "a", 1 } };

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, null!);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_ValidKey_ReturnsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, "b");

        // Assert
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_MissingKey_ReturnsDefault()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "a", 1 } };

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, "missing");

        // Assert
        Assert.AreEqual(default, result);
    }

    #endregion

    #region SafeArrayAccess - List Tests

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NullList_ReturnsDefault()
    {
        // Arrange
        IList<int>? list = null;

        // Act
        var result = SafeArrayAccess.GetListElement(list, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_EmptyList_ReturnsDefault()
    {
        // Arrange
        var list = new List<int>();

        // Act
        var result = SafeArrayAccess.GetListElement(list, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_ValidIndex_ReturnsElement()
    {
        // Arrange
        var list = new List<int> { 10, 20, 30 };

        // Act
        var result = SafeArrayAccess.GetListElement(list, 1);

        // Assert
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var list = new List<int> { 10, 20, 30 };

        // Act
        var result = SafeArrayAccess.GetListElement(list, -1);

        // Assert
        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_OutOfBounds_ReturnsDefault()
    {
        // Arrange
        var list = new List<int> { 10, 20 };

        // Act
        var result = SafeArrayAccess.GetListElement(list, 100);

        // Assert
        Assert.AreEqual(default, result);
    }

    #endregion

    #region SafeArrayAccess - GetIndexedElement Tests

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullIndexable_ReturnsDefault()
    {
        // Arrange & Act
        var result = SafeArrayAccess.GetIndexedElement(null, 0, typeof(string));

        // Assert - reference types return null
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullIndex_ReturnsDefault()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, null, typeof(string));

        // Assert - reference types return null
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_String_ValidIndex_ReturnsChar()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = SafeArrayAccess.GetIndexedElement(str, 0, typeof(char));

        // Assert
        Assert.AreEqual('H', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_ValidIndex_ReturnsElement()
    {
        // Arrange
        var array = new[] { 100, 200, 300 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, 1, typeof(int));

        // Assert
        Assert.AreEqual(200, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var array = new[] { 100, 200, 300 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, -1, typeof(int));

        // Assert
        Assert.AreEqual(300, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_OutOfBounds_ReturnsDefault()
    {
        // Arrange
        var array = new[] { 1, 2 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, 100, typeof(int));

        // Assert
        Assert.AreEqual(0, result); // Default for int
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_EmptyArray_ReturnsDefault()
    {
        // Arrange
        var array = Array.Empty<int>();

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, 0, typeof(int));

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Dictionary_ValidKey_ReturnsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 42 } };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(dict, "key1", typeof(int));

        // Assert
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Dictionary_MissingKey_ReturnsDefault()
    {
        // Arrange
        var dict = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(dict, "missing", typeof(string));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_List_ValidIndex_ReturnsElement()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(list, 1, typeof(string));

        // Assert
        Assert.AreEqual("b", result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullableType_ReturnsNull()
    {
        // Arrange & Act
        var result = SafeArrayAccess.GetIndexedElement(null, 0, typeof(int?));

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}
