using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseStrictConversionsDecimalTests
{
    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringDecimal_ShouldConvert()
    {
        var lib = new LibraryBase();
        
        var result = lib.TryConvertToDecimalStrict("100,50");
        
        Assert.IsNotNull(result, "Result should not be null");
        Assert.AreEqual(100.50m, result.Value, "Should parse 100,50 correctly");
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringDecimal_MatchesLiteral()
    {
        var lib = new LibraryBase();
        
        var result = lib.TryConvertToDecimalStrict("100,50");
        decimal literal = 100.50m;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(literal, result.Value, "Parsed value should match literal");
        Assert.AreEqual(literal, result.Value, "Equality comparison should work");
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringInteger_ShouldConvert()
    {
        var lib = new LibraryBase();
        
        var result = lib.TryConvertToDecimalStrict("100");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.Value);
    }
}
