using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class HashFunctionsTests : LibraryBaseBaseTests
{
    #region MD5 Tests

    [TestMethod]
    public void Md5Test()
    {
        Assert.AreEqual("098F6BCD4621D373CADE4E832627B4F6", Library.Md5("test"));
    }

    [TestMethod]
    public void Md5NullTest()
    {
        Assert.IsNull(Library.Md5((string?)null));
    }

    [TestMethod]
    public void Md5_WhenBytesProvided_ShouldReturnHash()
    {
        var bytes = Encoding.UTF8.GetBytes("test");
        var result = Library.Md5(bytes);

        Assert.AreEqual("098F6BCD4621D373CADE4E832627B4F6", result);
    }

    [TestMethod]
    public void Md5_WhenNullBytesProvided_ShouldReturnNull()
    {
        var result = Library.Md5((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Md5_WhenEmptyBytesProvided_ShouldReturnHash()
    {
        var result = Library.Md5(Array.Empty<byte>());

        Assert.IsNotNull(result);
        Assert.AreEqual(32, result.Length);
    }

    #endregion

    #region SHA1 Tests

    [TestMethod]
    public void Sha1Test()
    {
        Assert.AreEqual("A94A8FE5CCB19BA61C4C0873D391E987982FBBD3", Library.Sha1("test"));
    }

    [TestMethod]
    public void Sha1NullTest()
    {
        Assert.IsNull(Library.Sha1((string?)null));
    }

    [TestMethod]
    public void Sha1_WhenBytesProvided_ShouldReturnHash()
    {
        var bytes = Encoding.UTF8.GetBytes("test");
        var result = Library.Sha1(bytes);

        Assert.AreEqual("A94A8FE5CCB19BA61C4C0873D391E987982FBBD3", result);
    }

    [TestMethod]
    public void Sha1_WhenNullBytesProvided_ShouldReturnNull()
    {
        var result = Library.Sha1((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Sha1_WhenEmptyBytesProvided_ShouldReturnHash()
    {
        var result = Library.Sha1(Array.Empty<byte>());

        Assert.IsNotNull(result);
        Assert.AreEqual(40, result.Length);
    }

    #endregion

    #region SHA256 Tests

    [TestMethod]
    public void Sha256Test()
    {
        Assert.AreEqual("9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08", Library.Sha256("test"));
    }

    [TestMethod]
    public void Sha256NullTest()
    {
        Assert.IsNull(Library.Sha256((string?)null));
    }

    [TestMethod]
    public void Sha256_WhenBytesProvided_ShouldReturnHash()
    {
        var bytes = Encoding.UTF8.GetBytes("test");
        var result = Library.Sha256(bytes);

        Assert.AreEqual("9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08", result);
    }

    [TestMethod]
    public void Sha256_WhenNullBytesProvided_ShouldReturnNull()
    {
        var result = Library.Sha256((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Sha256_WhenEmptyBytesProvided_ShouldReturnHash()
    {
        var result = Library.Sha256(Array.Empty<byte>());

        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);
    }

    #endregion

    #region SHA512 Tests

    [TestMethod]
    public void Sha512Test()
    {
        Assert.AreEqual(
            "EE26B0DD4AF7E749AA1A8EE3C10AE9923F618980772E473F8819A5D4940E0DB27AC185F8A0E1D5F84F88BC887FD67B143732C304CC5FA9AD8E6F57F50028A8FF",
            Library.Sha512("test"));
    }

    [TestMethod]
    public void Sha512NullTest()
    {
        Assert.IsNull(Library.Sha512((string?)null));
    }

    [TestMethod]
    public void Sha512_WhenBytesProvided_ShouldReturnHash()
    {
        var bytes = Encoding.UTF8.GetBytes("test");
        var result = Library.Sha512(bytes);

        Assert.AreEqual(
            "EE26B0DD4AF7E749AA1A8EE3C10AE9923F618980772E473F8819A5D4940E0DB27AC185F8A0E1D5F84F88BC887FD67B143732C304CC5FA9AD8E6F57F50028A8FF",
            result);
    }

    [TestMethod]
    public void Sha512_WhenNullBytesProvided_ShouldReturnNull()
    {
        var result = Library.Sha512((byte[]?)null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Sha512_WhenEmptyBytesProvided_ShouldReturnHash()
    {
        var result = Library.Sha512(Array.Empty<byte>());

        Assert.IsNotNull(result);
        Assert.AreEqual(128, result.Length);
    }

    #endregion

    #region Consistency Tests

    [TestMethod]
    public void Hash_StringAndBytes_ShouldReturnSameHash()
    {
        var input = "hello world";
        var bytes = Encoding.UTF8.GetBytes(input);

        Assert.AreEqual(Library.Md5(input), Library.Md5(bytes));
        Assert.AreEqual(Library.Sha1(input), Library.Sha1(bytes));
        Assert.AreEqual(Library.Sha256(input), Library.Sha256(bytes));
        Assert.AreEqual(Library.Sha512(input), Library.Sha512(bytes));
    }

    [TestMethod]
    public void Hash_SameInput_ShouldReturnSameHash()
    {
        Assert.AreEqual(Library.Md5("test"), Library.Md5("test"));
        Assert.AreEqual(Library.Sha1("test"), Library.Sha1("test"));
        Assert.AreEqual(Library.Sha256("test"), Library.Sha256("test"));
        Assert.AreEqual(Library.Sha512("test"), Library.Sha512("test"));
    }

    [TestMethod]
    public void Hash_DifferentInput_ShouldReturnDifferentHash()
    {
        Assert.AreNotEqual(Library.Md5("test1"), Library.Md5("test2"));
        Assert.AreNotEqual(Library.Sha1("test1"), Library.Sha1("test2"));
        Assert.AreNotEqual(Library.Sha256("test1"), Library.Sha256("test2"));
        Assert.AreNotEqual(Library.Sha512("test1"), Library.Sha512("test2"));
    }

    #endregion
}