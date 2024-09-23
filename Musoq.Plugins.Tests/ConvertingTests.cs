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
        CultureInfo.CurrentCulture = new CultureInfo("gb-GB");

        Assert.AreEqual(12.323m, Library.ToDecimal("12.323"));
        Assert.AreEqual(-12.323m, Library.ToDecimal("-12.323"));
        Assert.AreEqual(null, Library.ToDecimal(""));

        CultureInfo.CurrentCulture = oldCulture;
    }

    [TestMethod]
    public void ToDecimalWithCultureTest()
    {
        Assert.AreEqual(1.23m, Library.ToDecimal("1,23", "pl-PL"));
        Assert.AreEqual(-1.23m, Library.ToDecimal("-1,23", "pl-PL"));
        Assert.AreEqual(1.23m, Library.ToDecimal("1.23", "gb-GB"));
        Assert.AreEqual(-1.23m, Library.ToDecimal("-1.23", "gb-GB"));
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
        Assert.AreEqual(null, Library.ToInt64((string)null));
    }

    [TestMethod]
    public void ToStringDateTimeOffsetTest()
    {
        Assert.AreEqual("01.01.2015 00:00:00 +00:00", Library.ToString(DateTimeOffset.Parse("01.01.2015 00:00:00 +00:00")));
        Assert.AreEqual(null, Library.ToString((DateTimeOffset?)null));
    }

    [TestMethod]
    public void ToStringDecimalTest()
    {
        Assert.AreEqual("32,22", Library.ToString(32.22m));
        Assert.AreEqual(null, Library.ToString((decimal?) null));
    }

    [TestMethod]
    public void ToStringLongTest()
    {
        Assert.AreEqual("32", Library.ToString(32L));
        Assert.AreEqual(null, Library.ToString((long?)null));
    }

    [TestMethod]
    public void ToStringObjectTest()
    {
        Assert.AreEqual("test class", Library.ToString(new TestToStringClass()));
        Assert.AreEqual(null, Library.ToString((TestToStringClass)null));
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