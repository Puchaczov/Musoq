using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DecimalParseTests
{
    [TestMethod]
    public void DecimalTryParse_WithComma_CurrentCulture_Polish()
    {
        var result = decimal.TryParse("100,50", out var value);
        Assert.IsTrue(result, $"Parse failed. CurrentCulture: {CultureInfo.CurrentCulture.Name}");
        Assert.AreEqual(100.50m, value);
    }

    [TestMethod]
    public void DecimalTryParse_WithDot_InvariantCulture()
    {
        var result = decimal.TryParse("100.50", NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
        Assert.IsTrue(result);
        Assert.AreEqual(100.50m, value);
    }
}
