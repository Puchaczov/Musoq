using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class MathTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void AbsDecimalTest()
    {
        Assert.AreEqual(112.5734m, Library.Abs(112.5734m));
        Assert.AreEqual(112.5734m, Library.Abs(-112.5734m));
        Assert.IsNull(Library.Abs((decimal?)null));
    }

    [TestMethod]
    public void AbsLongTest()
    {
        Assert.AreEqual(112L, Library.Abs(112L));
        Assert.AreEqual(112L, Library.Abs(-112L));
        Assert.IsNull(Library.Abs((long?)null));
    }

    [TestMethod]
    public void AbsIntTest()
    {
        Assert.AreEqual(112, Library.Abs(112));
        Assert.AreEqual(112, Library.Abs(-112));
        Assert.IsNull(Library.Abs(null));
    }

    [TestMethod]
    public void CeilTest()
    {
        Assert.AreEqual(113m, Library.Ceil(112.5734m));
        Assert.AreEqual(-112m, Library.Ceil(-112.5734m));
        Assert.IsNull(Library.Ceil(null));
    }

    [TestMethod]
    public void FloorTest()
    {
        Assert.AreEqual(112m, Library.Floor(112.5734m));
        Assert.AreEqual(-113m, Library.Floor(-112.5734m));
        Assert.IsNull(Library.Floor(null));
    }

    [TestMethod]
    public void SignDecimalTest()
    {
        Assert.AreEqual(1m, Library.Sign(13m));
        Assert.AreEqual(0m, Library.Sign(0m));
        Assert.AreEqual(-1m, Library.Sign(-13m));
        Assert.IsNull(Library.Sign((decimal?)null));
    }

    [TestMethod]
    public void SignLongTest()
    {
        Assert.AreEqual(1, Library.Sign(13));
        Assert.AreEqual(0, Library.Sign(0));
        Assert.AreEqual(-1, Library.Sign(-13));
        Assert.IsNull(Library.Sign(null));
    }

    [TestMethod]
    public void RoundTest()
    {
        Assert.AreEqual(2.1m, Library.Round(2.1351m, 1));
        Assert.IsNull(Library.Round(null, 1));
    }

    [TestMethod]
    public void PercentOfTest()
    {
        Assert.AreEqual(25m, Library.PercentOf(25, 100));
        Assert.IsNull(Library.PercentOf(null, 100));
        Assert.IsNull(Library.PercentOf(25, null));
        Assert.IsNull(Library.PercentOf(null, null));
    }

    [TestMethod]
    public void FromHexTest()
    {
        // Basic hex parsing
        Assert.AreEqual(255L, Library.FromHex("FF"));
        Assert.AreEqual(255L, Library.FromHex("ff"));
        Assert.AreEqual(10L, Library.FromHex("A"));
        Assert.AreEqual(16L, Library.FromHex("10"));
        
        // With 0x prefix
        Assert.AreEqual(255L, Library.FromHex("0xFF"));
        Assert.AreEqual(255L, Library.FromHex("0xff"));
        Assert.AreEqual(255L, Library.FromHex("0XFF"));
        
        // Negative values
        Assert.AreEqual(-1L, Library.FromHex("FFFFFFFFFFFFFFFF"));
        
        // Edge cases
        Assert.AreEqual(0L, Library.FromHex("0"));
        Assert.AreEqual(0L, Library.FromHex("0x0"));
        
        // Invalid inputs
        Assert.IsNull(Library.FromHex(null));
        Assert.IsNull(Library.FromHex(""));
        Assert.IsNull(Library.FromHex("   "));
        Assert.IsNull(Library.FromHex("GG"));
        Assert.IsNull(Library.FromHex("0xGG"));
    }

    [TestMethod]
    public void FromBinTest()
    {
        // Basic binary parsing
        Assert.AreEqual(5L, Library.FromBin("101"));
        Assert.AreEqual(10L, Library.FromBin("1010"));
        Assert.AreEqual(15L, Library.FromBin("1111"));
        
        // With 0b prefix
        Assert.AreEqual(5L, Library.FromBin("0b101"));
        Assert.AreEqual(5L, Library.FromBin("0B101"));
        
        // Edge cases
        Assert.AreEqual(0L, Library.FromBin("0"));
        Assert.AreEqual(0L, Library.FromBin("0b0"));
        Assert.AreEqual(1L, Library.FromBin("1"));
        
        // Invalid inputs
        Assert.IsNull(Library.FromBin(null));
        Assert.IsNull(Library.FromBin(""));
        Assert.IsNull(Library.FromBin("   "));
        Assert.IsNull(Library.FromBin("102"));
        Assert.IsNull(Library.FromBin("0b102"));
    }

    [TestMethod]
    public void FromOctTest()
    {
        // Basic octal parsing
        Assert.AreEqual(8L, Library.FromOct("10"));
        Assert.AreEqual(64L, Library.FromOct("100"));
        Assert.AreEqual(7L, Library.FromOct("7"));
        Assert.AreEqual(511L, Library.FromOct("777"));
        
        // With 0o prefix
        Assert.AreEqual(8L, Library.FromOct("0o10"));
        Assert.AreEqual(8L, Library.FromOct("0O10"));
        
        // Edge cases
        Assert.AreEqual(0L, Library.FromOct("0"));
        Assert.AreEqual(0L, Library.FromOct("0o0"));
        
        // Invalid inputs
        Assert.IsNull(Library.FromOct(null));
        Assert.IsNull(Library.FromOct(""));
        Assert.IsNull(Library.FromOct("   "));
        Assert.IsNull(Library.FromOct("8"));
        Assert.IsNull(Library.FromOct("0o8"));
    }
}