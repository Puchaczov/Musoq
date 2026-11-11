using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class FromBytesMethodsTests
{
    [TestMethod]
    public void FromBytesToBool_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToBool(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToBool_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToBool(System.Array.Empty<byte>());
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt16_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt16(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt16_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt16(new byte[1]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt16_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt16(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt16_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt16(new byte[1]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt32_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt32(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt32_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt32(new byte[3]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt32_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt32(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt32_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt32(new byte[3]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt64_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt64(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt64_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt64(new byte[7]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt64_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt64(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt64_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt64(new byte[7]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToFloat_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToFloat(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToFloat_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToFloat(new byte[3]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToDouble_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToDouble(null!);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToDouble_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToDouble(new byte[7]);
        Assert.IsFalse(result.HasValue);
    }
}
