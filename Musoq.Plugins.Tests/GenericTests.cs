using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class GenericTests : LibraryBaseBaseTests
{
    #region MergeArrays Tests (existing)
    
    [TestMethod]
    public void WhenMergeArraysWithSingleArgument_ShouldReturnArray()
    {
        var result = Library.MergeArrays("test1"u8.ToArray());

        Assert.IsNotNull(result);
        Assert.HasCount(5, result);
        Assert.AreEqual("test1", Encoding.UTF8.GetString(result));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithTwoArguments_ShouldReturnArray()
    {
        var result = Library.MergeArrays("test1"u8.ToArray(), "test2"u8.ToArray());

        Assert.IsNotNull(result);
        Assert.HasCount(10, result);
        Assert.AreEqual("test1test2", Encoding.UTF8.GetString(result));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithThreeArguments_ShouldReturnArray()
    {
        var result = Library.MergeArrays("test1"u8.ToArray(), "test2"u8.ToArray(), "test3"u8.ToArray());

        Assert.IsNotNull(result);
        Assert.HasCount(15, result);
        Assert.AreEqual("test1test2test3", Encoding.UTF8.GetString(result));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithNull_ShouldReturnNull()
    {
        var result = Library.MergeArrays((byte[][])null!);

        Assert.IsNull(result);
    }
    
    #endregion
    
    #region Skip Tests
    
    [TestMethod]
    public void Skip_WithValidInput_ShouldReturnCorrectElements()
    {
        // Arrange
        var values = new[] { 1, 2, 3, 4, 5 };
        
        // Act
        var result = Library.Skip(values, 2);
        
        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, result.ToArray());
    }
    
    [TestMethod]
    public void Skip_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        IEnumerable<int>? values = null;
        
        // Act
        var result = Library.Skip(values, 2);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void Skip_WithZeroCount_ShouldReturnOriginal()
    {
        // Arrange
        var values = new[] { 1, 2, 3 };
        
        // Act
        var result = Library.Skip(values, 0);
        
        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.ToArray());
    }
    
    #endregion
    
    #region Take Tests
    
    [TestMethod]
    public void Take_WithValidInput_ShouldReturnCorrectElements()
    {
        // Arrange
        var values = new[] { 1, 2, 3, 4, 5 };
        
        // Act
        var result = Library.Take(values, 3);
        
        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.ToArray());
    }
    
    [TestMethod]
    public void Take_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        IEnumerable<int>? values = null;
        
        // Act
        var result = Library.Take(values, 3);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void Take_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var values = new[] { 1, 2, 3 };
        
        // Act
        var result = Library.Take(values, 0);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }
    
    #endregion
    
    #region SkipAndTake Tests
    
    [TestMethod]
    public void SkipAndTake_WithValidInput_ShouldReturnCorrectElements()
    {
        // Arrange
        var values = new[] { 1, 2, 3, 4, 5, 6, 7 };
        
        // Act
        var result = Library.SkipAndTake(values, 2, 3);
        
        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, result.ToArray());
    }
    
    [TestMethod]
    public void SkipAndTake_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        IEnumerable<int>? values = null;
        
        // Act
        var result = Library.SkipAndTake(values, 2, 3);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region EnumerableToArray Tests
    
    [TestMethod]
    public void EnumerableToArray_WithValidInput_ShouldReturnArray()
    {
        // Arrange
        var values = new List<string> { "a", "b", "c" };
        
        // Act
        var result = Library.EnumerableToArray(values);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(3, result);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result);
    }
    
    [TestMethod]
    public void EnumerableToArray_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        IEnumerable<string>? values = null;
        
        // Act
        var result = Library.EnumerableToArray(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region Length Tests
    
    [TestMethod]
    public void Length_WithEnumerable_ShouldReturnCount()
    {
        // Arrange
        var values = new[] { 1, 2, 3, 4, 5 };
        
        // Act
        var result = Library.Length(values);
        
        // Assert
        Assert.AreEqual(5, result);
    }
    
    [TestMethod]
    public void Length_WithNullEnumerable_ShouldReturnNull()
    {
        // Arrange
        IEnumerable<int>? values = null;
        
        // Act
        var result = Library.Length(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void Length_WithArray_ShouldReturnLength()
    {
        // Arrange
        var values = new[] { "a", "b", "c" };
        
        // Act
        var result = Library.Length(values);
        
        // Assert
        Assert.AreEqual(3, result);
    }
    
    [TestMethod]
    public void Length_WithNullArray_ShouldReturnNull()
    {
        // Arrange
        string[]? values = null;
        
        // Act
        var result = Library.Length(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region GetElementAtOrDefault Tests
    
    [TestMethod]
    public void GetElementAtOrDefault_WithValidIndex_ShouldReturnElement()
    {
        // Arrange
        var values = new[] { "first", "second", "third" };
        
        // Act
        var result = Library.GetElementAtOrDefault(values, 1);
        
        // Assert
        Assert.AreEqual("second", result);
    }
    
    [TestMethod]
    public void GetElementAtOrDefault_WithInvalidIndex_ShouldReturnDefault()
    {
        // Arrange
        var values = new[] { "first", "second", "third" };
        
        // Act
        var result = Library.GetElementAtOrDefault(values, 10);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void GetElementAtOrDefault_WithNullCollection_ShouldReturnDefault()
    {
        // Arrange
        IEnumerable<string>? values = null;
        
        // Act
        var result = Library.GetElementAtOrDefault(values, 1);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void GetElementAtOrDefault_WithNullIndex_ShouldReturnDefault()
    {
        // Arrange
        var values = new[] { "first", "second", "third" };
        
        // Act
        var result = Library.GetElementAtOrDefault(values, null);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region Distinct Tests
    
    [TestMethod]
    public void Distinct_WithDuplicates_ShouldReturnUniqueElements()
    {
        // Arrange
        var values = new[] { 1, 2, 2, 3, 3, 3, 4 };
        
        // Act
        var result = Library.Distinct(values);
        
        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, result.ToArray());
    }
    
    [TestMethod]
    public void Distinct_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        IEnumerable<int>? values = null;
        
        // Act
        var result = Library.Distinct(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void Distinct_WithNoDuplicates_ShouldReturnOriginal()
    {
        // Arrange
        var values = new[] { 1, 2, 3, 4 };
        
        // Act
        var result = Library.Distinct(values);
        
        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, result.ToArray());
    }
    
    #endregion
    
    #region FirstOrDefault Tests
    
    [TestMethod]
    public void FirstOrDefault_WithElements_ShouldReturnFirst()
    {
        // Arrange
        var values = new[] { "first", "second", "third" };
        
        // Act
        var result = Library.FirstOrDefault(values);
        
        // Assert
        Assert.AreEqual("first", result);
    }
    
    [TestMethod]
    public void FirstOrDefault_WithEmptyCollection_ShouldReturnDefault()
    {
        // Arrange
        var values = new string[0];
        
        // Act
        var result = Library.FirstOrDefault(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void FirstOrDefault_WithNullInput_ShouldReturnDefault()
    {
        // Arrange
        IEnumerable<string>? values = null;
        
        // Act
        var result = Library.FirstOrDefault(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region LastOrDefault Tests
    
    [TestMethod]
    public void LastOrDefault_WithElements_ShouldReturnLast()
    {
        // Arrange
        var values = new[] { "first", "second", "third" };
        
        // Act
        var result = Library.LastOrDefault(values);
        
        // Assert
        Assert.AreEqual("third", result);
    }
    
    [TestMethod]
    public void LastOrDefault_WithEmptyCollection_ShouldReturnDefault()
    {
        // Arrange
        var values = new string[0];
        
        // Act
        var result = Library.LastOrDefault(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void LastOrDefault_WithNullInput_ShouldReturnDefault()
    {
        // Arrange
        IEnumerable<string>? values = null;
        
        // Act
        var result = Library.LastOrDefault(values);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region NthOrDefault Tests
    
    [TestMethod]
    public void NthOrDefault_WithValidIndex_ShouldReturnElement()
    {
        // Arrange
        var values = new[] { "zero", "one", "two", "three" };
        
        // Act
        var result = Library.NthOrDefault(values, 2);
        
        // Assert
        Assert.AreEqual("two", result);
    }
    
    [TestMethod]
    public void NthOrDefault_WithInvalidIndex_ShouldReturnDefault()
    {
        // Arrange
        var values = new[] { "zero", "one", "two" };
        
        // Act
        var result = Library.NthOrDefault(values, 10);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void NthOrDefault_WithNullInput_ShouldReturnDefault()
    {
        // Arrange
        IEnumerable<string>? values = null;
        
        // Act
        var result = Library.NthOrDefault(values, 2);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion
    
    #region NthFromEndOrDefault Tests
    
    [TestMethod]
    public void NthFromEndOrDefault_WithValidIndex_ShouldReturnElement()
    {
        // Arrange
        var values = new[] { "zero", "one", "two", "three" };
        
        // Act
        var result = Library.NthFromEndOrDefault(values, 1); // Second from end
        
        // Assert
        Assert.AreEqual("two", result);
    }
    
    [TestMethod]
    public void NthFromEndOrDefault_WithInvalidIndex_ShouldReturnDefault()
    {
        // Arrange
        var values = new[] { "zero", "one", "two" };
        
        // Act
        var result = Library.NthFromEndOrDefault(values, 10);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void NthFromEndOrDefault_WithNullInput_ShouldReturnDefault()
    {
        // Arrange
        IEnumerable<string>? values = null;
        
        // Act
        var result = Library.NthFromEndOrDefault(values, 1);
        
        // Assert
        Assert.IsNull(result);
    }
    
    #endregion

    #region NullIf Tests

    [TestMethod]
    public void NullIf_WhenValuesEqual_ReturnsNull()
    {
        Assert.IsNull(Library.NullIf("test", "test"));
    }

    [TestMethod]
    public void NullIf_WhenValuesNotEqual_ReturnsFirstValue()
    {
        Assert.AreEqual("test", Library.NullIf("test", "other"));
    }

    [TestMethod]
    public void NullIf_WhenFirstValueNull_ReturnsDefault()
    {
        Assert.IsNull(Library.NullIf<string>(null, "test"));
    }

    [TestMethod]
    public void NullIf_WhenSecondValueNull_ReturnsFirstValue()
    {
        Assert.AreEqual("test", Library.NullIf<string>("test", null));
    }

    [TestMethod]
    public void NullIf_WhenBothNull_ReturnsDefault()
    {
        Assert.IsNull(Library.NullIf<string>(null, null));
    }

    [TestMethod]
    public void NullIf_WithIntegers_WhenEqual_ReturnsDefault()
    {
        
        Assert.AreEqual(0, Library.NullIf(5, 5));
    }

    [TestMethod]
    public void NullIf_WithIntegers_WhenNotEqual_ReturnsFirstValue()
    {
        Assert.AreEqual(5, Library.NullIf(5, 10));
    }

    #endregion

    #region IfNull Tests

    [TestMethod]
    public void IfNull_WhenFirstValueNotNull_ReturnsFirstValue()
    {
        Assert.AreEqual("test", Library.IfNull("test", "default"));
    }

    [TestMethod]
    public void IfNull_WhenFirstValueNull_ReturnsSecondValue()
    {
        Assert.AreEqual("default", Library.IfNull<string>(null, "default"));
    }

    [TestMethod]
    public void IfNull_WhenBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.IfNull<string>(null, null));
    }

    [TestMethod]
    public void IfNull_WithIntegers_WhenNotNull_ReturnsFirstValue()
    {
        Assert.AreEqual(5, Library.IfNull<int?>(5, 10));
    }

    [TestMethod]
    public void IfNull_WithIntegers_WhenNull_ReturnsSecondValue()
    {
        Assert.AreEqual(10, Library.IfNull<int?>(null, 10));
    }

    #endregion

    #region DefaultIfNull Tests

    [TestMethod]
    public void DefaultIfNull_WhenValueNotNull_ReturnsValue()
    {
        Assert.AreEqual("test", Library.DefaultIfNull<string>("test"));
    }

    [TestMethod]
    public void DefaultIfNull_WhenValueNull_ReturnsDefault()
    {
        Assert.IsNull(Library.DefaultIfNull<string>(null));
    }

    [TestMethod]
    public void DefaultIfNull_WithInteger_WhenNull_ReturnsNull()
    {
        
        Assert.IsNull(Library.DefaultIfNull<int?>(null));
    }

    #endregion

    #region IsNull Tests

    [TestMethod]
    public void IsNull_WhenValueNull_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNull<string>(null));
    }

    [TestMethod]
    public void IsNull_WhenValueNotNull_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNull("test"));
    }

    [TestMethod]
    public void IsNull_WithInteger_WhenNull_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNull<int?>(null));
    }

    #endregion

    #region IsNotNull Tests

    [TestMethod]
    public void IsNotNull_WhenValueNotNull_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNotNull("test"));
    }

    [TestMethod]
    public void IsNotNull_WhenValueNull_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNotNull<string>(null));
    }

    #endregion
}