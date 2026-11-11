using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsTests
{
    #region ExtractUrls Tests

    [TestMethod]
    public void ExtractUrls_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ExtractUrls(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractUrls_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ExtractUrls(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractUrls_WhenNoUrlsProvided_ShouldReturnEmpty()
    {
        var result = Library.ExtractUrls("no urls here");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ExtractUrls_WhenSingleUrlProvided_ShouldExtract()
    {
        var result = Library.ExtractUrls("Visit https://example.com today");

        Assert.AreEqual("https://example.com", result);
    }

    [TestMethod]
    public void ExtractUrls_WhenMultipleUrlsProvided_ShouldExtractAll()
    {
        var result = Library.ExtractUrls("Visit https://example.com and http://test.com");

        Assert.IsTrue(result?.Contains("https://example.com"));
        Assert.IsTrue(result?.Contains("http://test.com"));
    }

    #endregion

    #region ExtractEmails Tests

    [TestMethod]
    public void ExtractEmails_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ExtractEmails(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractEmails_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ExtractEmails(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractEmails_WhenNoEmailsProvided_ShouldReturnEmpty()
    {
        var result = Library.ExtractEmails("no emails here");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ExtractEmails_WhenSingleEmailProvided_ShouldExtract()
    {
        var result = Library.ExtractEmails("Contact test@example.com for info");

        Assert.AreEqual("test@example.com", result);
    }

    [TestMethod]
    public void ExtractEmails_WhenMultipleEmailsProvided_ShouldExtractAll()
    {
        var result = Library.ExtractEmails("Contact test@example.com or admin@example.com");

        Assert.IsTrue(result?.Contains("test@example.com"));
        Assert.IsTrue(result?.Contains("admin@example.com"));
    }

    #endregion

    #region ExtractIPs Tests

    [TestMethod]
    public void ExtractIPs_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ExtractIPs(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractIPs_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ExtractIPs(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractIPs_WhenNoIPsProvided_ShouldReturnEmpty()
    {
        var result = Library.ExtractIPs("no ips here");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ExtractIPs_WhenSingleIPProvided_ShouldExtract()
    {
        var result = Library.ExtractIPs("Server at 192.168.1.1 is running");

        Assert.AreEqual("192.168.1.1", result);
    }

    [TestMethod]
    public void ExtractIPs_WhenMultipleIPsProvided_ShouldExtractAll()
    {
        var result = Library.ExtractIPs("Servers 192.168.1.1 and 10.0.0.1 are running");

        Assert.IsTrue(result?.Contains("192.168.1.1"));
        Assert.IsTrue(result?.Contains("10.0.0.1"));
    }

    #endregion
}
