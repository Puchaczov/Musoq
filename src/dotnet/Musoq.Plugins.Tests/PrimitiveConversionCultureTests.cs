using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class PrimitiveConversionCultureTests : PluginsTestBase
{
    [TestMethod]
    public void ToDecimal_DefaultOverload_UsesInvariantCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("gb-GB")
            {
                NumberFormat =
                {
                    NumberDecimalSeparator = ",",
                    NumberGroupSeparator = "."
                }
            };

            Assert.AreEqual(12.323m, Library.ToDecimal("12.323"));
            Assert.AreEqual(-12.323m, Library.ToDecimal("-12.323"));
            Assert.IsNull(Library.ToDecimal("12,323"));
            Assert.IsNull(Library.ToDecimal(string.Empty));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
