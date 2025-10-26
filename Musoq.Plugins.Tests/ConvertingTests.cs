using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ConvertingTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void ToDecimalTest()
    {
        var oldCulture = CultureInfo.CurrentCulture;
        var culture = new CultureInfo("gb-GB")
        {
            NumberFormat =
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "."
            }
        };

        CultureInfo.CurrentCulture = culture;

        Assert.AreEqual(12.323m, Library.ToDecimal("12,323"));
        Assert.AreEqual(-12.323m, Library.ToDecimal("-12,323"));
        Assert.IsNull(Library.ToDecimal(string.Empty));

        CultureInfo.CurrentCulture = oldCulture;
    }

    [TestMethod]
    public void ToDecimalWithCultureTest()
    {
        var culture = CultureInfo.GetCultureInfo("gb-GB");
        
        Assert.AreEqual(1.23m, Library.ToDecimal("1,23", "pl-PL"));
        Assert.AreEqual(-1.23m, Library.ToDecimal("-1,23", "pl-PL"));
        Assert.AreEqual(1.23m, Library.ToDecimal($"1{culture.NumberFormat.NumberDecimalSeparator}23", "gb-GB"));
        Assert.AreEqual(-1.23m, Library.ToDecimal($"-1{culture.NumberFormat.NumberDecimalSeparator}23", "gb-GB"));
    }

    [TestMethod]
    public void ToDecimalLongTest()
    {
        Assert.AreEqual(64m, Library.ToDecimal(64L));
    }

    [TestMethod]
    public void ToLongTest()
    {
        Assert.AreEqual(12321L, Library.ToInt64("12321"));
        Assert.IsNull(Library.ToInt64((string?)null));
    }

    [TestMethod]
    public void ToStringDateTimeOffsetTest()
    {
        Assert.AreEqual("01.01.2015 00:00:00 +00:00", Library.ToString(DateTimeOffset.Parse("01.01.2015 00:00:00 +00:00"), "dd.MM.yyyy HH:mm:ss zzz"));
        Assert.IsNull(Library.ToString((DateTimeOffset?)null));
    }

    [TestMethod]
    public void ToStringDecimalTest()
    {
        Assert.AreEqual("32,22", Library.ToString(32.22m));
        Assert.IsNull(Library.ToString((decimal?) null));
    }

    [TestMethod]
    public void ToStringLongTest()
    {
        Assert.AreEqual("32", Library.ToString(32L));
        Assert.IsNull(Library.ToString((long?)null));
    }

    [TestMethod]
    public void ToStringObjectTest()
    {
        Assert.AreEqual("test class", Library.ToString(new TestToStringClass()));
        Assert.IsNull(Library.ToString((TestToStringClass?)null));
    }

    [TestMethod]
    public void ToBinTest()
    {
        Assert.AreEqual("100", Library.ToBin(4));
    }

    private class TestToStringClass
    {
        public override string ToString()
        {
            return "test class";
        }
    }
}