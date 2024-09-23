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
        Assert.AreEqual(null, Library.Abs((decimal?)null));
    }

    [TestMethod]
    public void AbsLongTest()
    {
        Assert.AreEqual(112L, Library.Abs(112L));
        Assert.AreEqual(112L, Library.Abs(-112L));
        Assert.AreEqual(null, Library.Abs((long?)null));
    }

    [TestMethod]
    public void AbsIntTest()
    {
        Assert.AreEqual(112, Library.Abs(112));
        Assert.AreEqual(112, Library.Abs(-112));
        Assert.AreEqual(null, Library.Abs(null));
    }

    [TestMethod]
    public void CeilTest()
    {
        Assert.AreEqual(113m, Library.Ceil(112.5734m));
        Assert.AreEqual(-112m, Library.Ceil(-112.5734m));
        Assert.AreEqual(null, Library.Ceil(null));
    }

    [TestMethod]
    public void FloorTest()
    {
        Assert.AreEqual(112m, Library.Floor(112.5734m));
        Assert.AreEqual(-113m, Library.Floor(-112.5734m));
        Assert.AreEqual(null, Library.Floor(null));
    }

    [TestMethod]
    public void SignDecimalTest()
    {
        Assert.AreEqual(1m, Library.Sign(13m));
        Assert.AreEqual(0m, Library.Sign(0m));
        Assert.AreEqual(-1m, Library.Sign(-13m));
        Assert.AreEqual(null, Library.Sign((decimal?)null));
    }

    [TestMethod]
    public void SignLongTest()
    {
        Assert.AreEqual(1, Library.Sign(13));
        Assert.AreEqual(0, Library.Sign(0));
        Assert.AreEqual(-1, Library.Sign(-13));
        Assert.AreEqual(null, Library.Sign(null));
    }

    [TestMethod]
    public void RoundTest()
    {
        Assert.AreEqual(2.1m, Library.Round(2.1351m, 1));
        Assert.AreEqual(null, Library.Round(null, 1));
    }

    [TestMethod]
    public void PercentOfTest()
    {
        Assert.AreEqual(25m, Library.PercentOf(25, 100));
        Assert.AreEqual(null, Library.PercentOf(null, 100));
        Assert.AreEqual(null, Library.PercentOf(25, null));
        Assert.AreEqual(null, Library.PercentOf(null, null));
    }
}