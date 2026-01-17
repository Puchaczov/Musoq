using System;
using System.Globalization;
using System.Text;
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
        Assert.AreEqual("01.01.2015 00:00:00 +00:00",
            Library.ToString(DateTimeOffset.Parse("01.01.2015 00:00:00 +00:00"), "dd.MM.yyyy HH:mm:ss zzz"));
        Assert.IsNull(Library.ToString((DateTimeOffset?)null));
    }

    [TestMethod]
    public void ToStringDecimalTest()
    {
        Assert.AreEqual("32,22", Library.ToString(32.22m));
        Assert.IsNull(Library.ToString((decimal?)null));
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

    #region Base64 Tests

    [TestMethod]
    public void ToBase64_FromBytes_ShouldReturnBase64String()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = Library.ToBase64(bytes);

        Assert.AreEqual("SGVsbG8=", result);
    }

    [TestMethod]
    public void ToBase64_FromNullBytes_ShouldReturnNull()
    {
        var result = Library.ToBase64((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBase64_FromBytesWithOffsetAndLength_ShouldReturnBase64String()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello World");
        var result = Library.ToBase64(bytes, 0, 5);

        Assert.AreEqual("SGVsbG8=", result);
    }

    [TestMethod]
    public void ToBase64_FromNullBytesWithOffsetAndLength_ShouldReturnNull()
    {
        var result = Library.ToBase64(null, 0, 5);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBase64_FromString_ShouldReturnBase64String()
    {
        var result = Library.ToBase64("Hello");

        Assert.AreEqual("SGVsbG8=", result);
    }

    [TestMethod]
    public void ToBase64_FromNullString_ShouldReturnNull()
    {
        var result = Library.ToBase64((string?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBase64_FromStringWithEncoding_ShouldReturnBase64String()
    {
        var result = Library.ToBase64("Hello", "UTF-8");

        Assert.AreEqual("SGVsbG8=", result);
    }

    [TestMethod]
    public void ToBase64_FromNullStringWithEncoding_ShouldReturnNull()
    {
        var result = Library.ToBase64(null, "UTF-8");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64_ShouldReturnBytes()
    {
        var result = Library.FromBase64("SGVsbG8=");

        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromBase64_WhenNull_ShouldReturnNull()
    {
        var result = Library.FromBase64(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64_WhenEmpty_ShouldReturnNull()
    {
        var result = Library.FromBase64(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_ShouldReturnDecodedString()
    {
        var result = Library.FromBase64ToString("SGVsbG8=");

        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void FromBase64ToString_WhenNull_ShouldReturnNull()
    {
        var result = Library.FromBase64ToString(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_WhenEmpty_ShouldReturnNull()
    {
        var result = Library.FromBase64ToString(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_ShouldReturnDecodedString()
    {
        var result = Library.FromBase64ToString("SGVsbG8=", "UTF-8");

        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void FromBase64ToString_WithEncodingWhenNull_ShouldReturnNull()
    {
        var result = Library.FromBase64ToString(null, "UTF-8");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Base64RoundTrip_String_ShouldPreserveContent()
    {
        const string original = "Hello, World! 日本語 🌍";

        var encoded = Library.ToBase64(original);
        var decoded = Library.FromBase64ToString(encoded);

        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void Base64RoundTrip_Bytes_ShouldPreserveContent()
    {
        var original = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };

        var encoded = Library.ToBase64(original);
        var decoded = Library.FromBase64(encoded);

        CollectionAssert.AreEqual(original, decoded);
    }

    #endregion
}