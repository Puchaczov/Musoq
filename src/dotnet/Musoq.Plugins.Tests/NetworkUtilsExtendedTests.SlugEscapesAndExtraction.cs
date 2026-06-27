using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsExtendedTests
{
    #region ToSlug Tests

    [TestMethod]
    public void ToSlug_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToSlug(null));
    }

    [TestMethod]
    public void ToSlug_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ToSlug(string.Empty));
    }

    [TestMethod]
    public void ToSlug_SimpleString_ReturnsLowercase()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello World"));
    }

    [TestMethod]
    public void ToSlug_WithSpecialChars_RemovesThem()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello! World?"));
    }

    [TestMethod]
    public void ToSlug_WithDashes_PreservesThemAsSlug()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello-World"));
    }

    [TestMethod]
    public void ToSlug_WithUnderscores_ConvertsToDashes()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello_World"));
    }

    [TestMethod]
    public void ToSlug_WithMultipleSpaces_UseSingleDash()
    {
        Assert.AreEqual("hello-world", Library.ToSlug("Hello   World"));
    }

    [TestMethod]
    public void ToSlug_WithAccents_RemovesAccents()
    {
        Assert.AreEqual("cafe", Library.ToSlug("Café"));
    }

    #endregion

    #region EscapeRegex Tests

    [TestMethod]
    public void EscapeRegex_Null_ReturnsNull()
    {
        Assert.IsNull(Library.EscapeRegex(null));
    }

    [TestMethod]
    public void EscapeRegex_NormalString_ReturnsUnchanged()
    {
        Assert.AreEqual("hello", Library.EscapeRegex("hello"));
    }

    [TestMethod]
    public void EscapeRegex_SpecialChars_EscapesThem()
    {
        Assert.AreEqual(@"\[test]", Library.EscapeRegex("[test]"));
    }

    #endregion

    #region EscapeSql Tests

    [TestMethod]
    public void EscapeSql_Null_ReturnsNull()
    {
        Assert.IsNull(Library.EscapeSql(null));
    }

    [TestMethod]
    public void EscapeSql_NoQuotes_ReturnsUnchanged()
    {
        Assert.AreEqual("hello", Library.EscapeSql("hello"));
    }

    [TestMethod]
    public void EscapeSql_SingleQuote_Doubled()
    {
        Assert.AreEqual("it''s", Library.EscapeSql("it's"));
    }

    [TestMethod]
    public void EscapeSql_MultipleQuotes_AllDoubled()
    {
        Assert.AreEqual("''test''", Library.EscapeSql("'test'"));
    }

    #endregion

    #region ExtractUrls Tests

    [TestMethod]
    public void ExtractUrls_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractUrls(null));
    }

    [TestMethod]
    public void ExtractUrls_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractUrls(string.Empty));
    }

    [TestMethod]
    public void ExtractUrls_NoUrls_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ExtractUrls("no urls here"));
    }

    [TestMethod]
    public void ExtractUrls_SingleUrl_ReturnsUrl()
    {
        Assert.AreEqual("https://example.com", Library.ExtractUrls("Visit https://example.com today"));
    }

    [TestMethod]
    public void ExtractUrls_MultipleUrls_ReturnsCommaSeparated()
    {
        var result = Library.ExtractUrls("Visit https://a.com and http://b.com");
        Assert.IsNotNull(result);
        Assert.Contains("https://a.com", result);
        Assert.Contains("http://b.com", result);
    }

    #endregion

    #region ExtractEmails Tests

    [TestMethod]
    public void ExtractEmails_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractEmails(null));
    }

    [TestMethod]
    public void ExtractEmails_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractEmails(string.Empty));
    }

    [TestMethod]
    public void ExtractEmails_NoEmails_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ExtractEmails("no emails here"));
    }

    [TestMethod]
    public void ExtractEmails_SingleEmail_ReturnsEmail()
    {
        Assert.AreEqual("test@example.com", Library.ExtractEmails("Contact test@example.com"));
    }

    [TestMethod]
    public void ExtractEmails_MultipleEmails_ReturnsCommaSeparated()
    {
        var result = Library.ExtractEmails("Contact a@b.com or c@d.com");
        Assert.IsNotNull(result);
        Assert.Contains("a@b.com", result);
        Assert.Contains("c@d.com", result);
    }

    #endregion

    #region ExtractIPs Tests

    [TestMethod]
    public void ExtractIPs_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractIPs(null));
    }

    [TestMethod]
    public void ExtractIPs_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractIPs(string.Empty));
    }

    [TestMethod]
    public void ExtractIPs_NoIPs_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ExtractIPs("no ips here"));
    }

    [TestMethod]
    public void ExtractIPs_SingleIP_ReturnsIP()
    {
        Assert.AreEqual("192.168.0.1", Library.ExtractIPs("Server at 192.168.0.1"));
    }

    [TestMethod]
    public void ExtractIPs_MultipleIPs_ReturnsCommaSeparated()
    {
        var result = Library.ExtractIPs("Servers 192.168.0.1 and 10.0.0.1");
        Assert.IsNotNull(result);
        Assert.Contains("192.168.0.1", result);
        Assert.Contains("10.0.0.1", result);
    }

    #endregion

    #region NewGuid Tests

    [TestMethod]
    public void NewGuid_ReturnsValidGuid()
    {
        var result = Library.NewGuid();
        Assert.IsTrue(Guid.TryParse(result, out _));
    }

    [TestMethod]
    public void NewGuid_ReturnsUnique()
    {
        var guid1 = Library.NewGuid();
        var guid2 = Library.NewGuid();
        Assert.AreNotEqual(guid1, guid2);
    }

    [TestMethod]
    public void NewGuidCompact_ReturnsGuidWithoutDashes()
    {
        var result = Library.NewGuidCompact();
        Assert.DoesNotContain("-", result);
        Assert.AreEqual(32, result.Length);
    }

    #endregion
}
