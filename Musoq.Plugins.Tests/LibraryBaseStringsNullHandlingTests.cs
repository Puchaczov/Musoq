using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
/// Tests for null handling in LibraryBase string methods.
/// Ensures methods return safe values instead of throwing exceptions when dealing with nulls.
/// </summary>
[TestClass]
public class LibraryBaseStringsNullHandlingTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void Split_WhenValueIsNull_ShouldNotThrow()
    {
        // Act
        var result = Library.Split(null, ",");
        
        // Assert - should return empty array instead of throwing
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }
    
    [TestMethod]
    public void Split_WhenValueIsNullWithMultipleSeparators_ShouldNotThrow()
    {
        // Act
        var result = Library.Split(null, ",", ";", "|");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }
    
    [TestMethod]
    public void ToCharArray_WhenValueIsNull_ShouldNotThrow()
    {
        // Act
        var result = Library.ToCharArray(null);
        
        // Assert - should return empty array instead of throwing
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }
    
    [TestMethod]
    public void Split_WhenValueIsValid_ShouldWork()
    {
        // Act
        var result = Library.Split("a,b,c", ",");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("a", result[0]);
        Assert.AreEqual("b", result[1]);
        Assert.AreEqual("c", result[2]);
    }
    
    [TestMethod]
    public void ToCharArray_WhenValueIsValid_ShouldWork()
    {
        // Act
        var result = Library.ToCharArray("abc");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual('a', result[0]);
        Assert.AreEqual('b', result[1]);
        Assert.AreEqual('c', result[2]);
    }
}
