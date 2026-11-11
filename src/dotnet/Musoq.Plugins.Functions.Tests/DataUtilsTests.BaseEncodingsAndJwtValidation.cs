using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DataUtilsTests
{
    #region IsBase64 Tests

    [TestMethod]
    public void IsBase64_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsBase64(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsBase64_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.IsBase64(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsBase64_WhenValidBase64Provided_ShouldReturnTrue()
    {
        var result = Library.IsBase64("SGVsbG8gV29ybGQh");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBase64_WhenValidBase64WithPaddingProvided_ShouldReturnTrue()
    {
        var result = Library.IsBase64("SGVsbG8=");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBase64_WhenInvalidLengthProvided_ShouldReturnFalse()
    {
        var result = Library.IsBase64("abc");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsBase64_WhenInvalidCharsProvided_ShouldReturnFalse()
    {
        var result = Library.IsBase64("!!!!");

        Assert.IsFalse(result);
    }

    #endregion

    #region IsHex Tests

    [TestMethod]
    public void IsHex_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsHex(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsHex_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.IsHex(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsHex_WhenValidHexLowercaseProvided_ShouldReturnTrue()
    {
        var result = Library.IsHex("0123456789abcdef");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WhenValidHexUppercaseProvided_ShouldReturnTrue()
    {
        var result = Library.IsHex("0123456789ABCDEF");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WhenMixedCaseProvided_ShouldReturnTrue()
    {
        var result = Library.IsHex("AbCdEf");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WhenInvalidCharsProvided_ShouldReturnFalse()
    {
        var result = Library.IsHex("GHIJKL");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsHex_WhenMixedValidInvalidProvided_ShouldReturnFalse()
    {
        var result = Library.IsHex("ABC123XYZ");

        Assert.IsFalse(result);
    }

    #endregion

    #region IsJwt Tests

    [TestMethod]
    public void IsJwt_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsJwt(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsJwt_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.IsJwt(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsJwt_WhenValidJwtProvided_ShouldReturnTrue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.IsJwt(jwt);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsJwt_WhenOnlyTwoPartsProvided_ShouldReturnFalse()
    {
        var result = Library.IsJwt("part1.part2");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJwt_WhenFourPartsProvided_ShouldReturnFalse()
    {
        var result = Library.IsJwt("part1.part2.part3.part4");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJwt_WhenInvalidBase64InParts_ShouldReturnFalse()
    {
        var result = Library.IsJwt("!!!.!!!.!!!");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJwt_WhenNotAJwtFormat_ShouldReturnFalse()
    {
        var result = Library.IsJwt("this is not a jwt");

        Assert.IsFalse(result);
    }

    #endregion
}
