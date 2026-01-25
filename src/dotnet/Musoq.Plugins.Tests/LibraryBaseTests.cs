using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void RowNumber_ShouldReturnRowNumber()
    {
        // Arrange - Create a QueryStats with known row number
        // Since RowNumber setter is protected, we create a testable QueryStats
        var stats = new TestableQueryStats(5);

        // Act
        var result = Library.RowNumber(stats);

        // Assert
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void GetTypeName_WithString_ShouldReturnStringTypeName()
    {
        // Arrange
        object obj = "test string";

        // Act
        var result = Library.GetTypeName(obj);

        // Assert
        Assert.AreEqual("System.String", result);
    }

    [TestMethod]
    public void GetTypeName_WithInt_ShouldReturnIntTypeName()
    {
        // Arrange
        object obj = 42;

        // Act
        var result = Library.GetTypeName(obj);

        // Assert
        Assert.AreEqual("System.Int32", result);
    }

    [TestMethod]
    public void GetTypeName_WithNull_ShouldReturnNull()
    {
        // Arrange
        object? obj = null;

        // Act
        var result = Library.GetTypeName(obj);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetTypeName_WithCustomObject_ShouldReturnCorrectTypeName()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = Library.GetTypeName(obj);

        // Assert
        Assert.IsTrue(result?.Contains("TestClass"));
    }

    [TestMethod]
    public void GetTypeName_WithArray_ShouldReturnArrayTypeName()
    {
        // Arrange
        object obj = new[] { 1, 2, 3 };

        // Act
        var result = Library.GetTypeName(obj);

        // Assert
        Assert.AreEqual("System.Int32[]", result);
    }

    private class TestClass
    {
        public string Name { get; set; } = "Test";
    }

    private class TestableQueryStats : QueryStats
    {
        public TestableQueryStats(int rowNumber)
        {
            InternalRowNumber = rowNumber;
        }
    }
}
