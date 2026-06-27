using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public partial class DataUtilsTests : PluginsTestBase
{
    #region JwtDecode Tests

    [TestMethod]
    public void JwtDecode_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenInvalidFormatProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode("not-a-jwt");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenOnlyOnePartProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode("eyJhbGciOiJIUzI1NiJ9");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenValidJwtProvided_ShouldReturnPayload()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtDecode(jwt);

        Assert.IsNotNull(result);
        Assert.Contains("1234567890", result);
        Assert.Contains("John Doe", result);
    }

    [TestMethod]
    public void JwtDecode_WhenInvalidBase64Provided_ShouldReturnNull()
    {
        var result = Library.JwtDecode("invalid.!!!invalid!!!.token");

        Assert.IsNull(result);
    }

    #endregion

    #region JwtGetHeader Tests

    [TestMethod]
    public void JwtGetHeader_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetHeader(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetHeader_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetHeader(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetHeader_WhenValidJwtProvided_ShouldReturnHeader()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetHeader(jwt);

        Assert.IsNotNull(result);
        Assert.Contains("HS256", result);
        Assert.Contains("JWT", result);
    }

    [TestMethod]
    public void JwtGetHeader_WhenNoPartsProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetHeader("");

        Assert.IsNull(result);
    }

    #endregion

    #region JwtGetClaim Tests

    [TestMethod]
    public void JwtGetClaim_WhenNullTokenProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetClaim(null, "sub");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenNullClaimNameProvided_ShouldReturnNull()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var result = Library.JwtGetClaim(jwt, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenEmptyTokenProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetClaim(string.Empty, "sub");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenEmptyClaimNameProvided_ShouldReturnNull()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var result = Library.JwtGetClaim(jwt, string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenValidClaimRequested_ShouldReturnValue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetClaim(jwt, "sub");

        Assert.AreEqual("1234567890", result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenStringClaimRequested_ShouldReturnString()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetClaim(jwt, "name");

        Assert.AreEqual("John Doe", result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenNumericClaimRequested_ShouldReturnRawValue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetClaim(jwt, "iat");

        Assert.AreEqual("1516239022", result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenNonExistentClaimRequested_ShouldReturnNull()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var result = Library.JwtGetClaim(jwt, "nonexistent");

        Assert.IsNull(result);
    }

    #endregion

}
