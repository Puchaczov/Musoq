using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class SumTests : LibraryBaseBaseTests
{
    #region Existing Tests

    [TestMethod]
    public void SumIntTest()
    {
        Library.SetSum(Group, "test", 1);
        Library.SetSum(Group, "test", 4);
        Library.SetSum(Group, "test", 6);
        Library.SetSum(Group, "test", (int?)null);

        Assert.AreEqual(11m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumIntParentTest()
    {
        Library.SetSum(Group, "test", 1, 1);
        Library.SetSum(Group, "test", 4, 1);
        Library.SetSum(Group, "test", 6);
        Library.SetSum(Group, "test", (int?)null);

        Assert.AreEqual(5, Library.Sum(Group, "test", 1));
        Assert.AreEqual(6, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumLongTest()
    {
        Library.SetSum(Group, "test", 1L);
        Library.SetSum(Group, "test", 4L);
        Library.SetSum(Group, "test", 6L);
        Library.SetSum(Group, "test", (long?)null);

        Assert.AreEqual(11m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumLongParentTest()
    {
        Library.SetSum(Group, "test", 1L, 1);
        Library.SetSum(Group, "test", 4L, 1);
        Library.SetSum(Group, "test", 6L);
        Library.SetSum(Group, "test", (long?)null, 1);

        Assert.AreEqual(5m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(6m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumDecimalTest()
    {
        Library.SetSum(Group, "test", 1m);
        Library.SetSum(Group, "test", 2m);
        Library.SetSum(Group, "test", 3m);
        Library.SetSum(Group, "test", (decimal?)null);

        Assert.AreEqual(6m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumDecimalParentTest()
    {
        Library.SetSum(Group, "test", 1m, 1);
        Library.SetSum(Group, "test", 4m, 1);
        Library.SetSum(Group, "test", 6m);
        Library.SetSum(Group, "test", (decimal?)null, 1);

        Assert.AreEqual(5m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(6m, Library.Sum(Group, "test"));
    }

    #endregion

    #region Byte Tests

    [TestMethod]
    public void SumByteTest()
    {
        Library.SetSum(Group, "test", (byte)10);
        Library.SetSum(Group, "test", (byte)20);
        Library.SetSum(Group, "test", (byte)30);
        Library.SetSum(Group, "test", (byte?)null);

        Assert.AreEqual(60m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumByteParentTest()
    {
        Library.SetSum(Group, "test", (byte)10, 1);
        Library.SetSum(Group, "test", (byte)20, 1);
        Library.SetSum(Group, "test", (byte)50);
        Library.SetSum(Group, "test", (byte?)null, 1);

        Assert.AreEqual(30m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(50m, Library.Sum(Group, "test"));
    }

    #endregion

    #region SByte Tests

    [TestMethod]
    public void SumSByteTest()
    {
        Library.SetSum(Group, "test", (sbyte)10);
        Library.SetSum(Group, "test", (sbyte)-5);
        Library.SetSum(Group, "test", (sbyte)15);
        Library.SetSum(Group, "test", (sbyte?)null);

        Assert.AreEqual(20m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumSByteParentTest()
    {
        Library.SetSum(Group, "test", (sbyte)5, 1);
        Library.SetSum(Group, "test", (sbyte)10, 1);
        Library.SetSum(Group, "test", (sbyte)20);
        Library.SetSum(Group, "test", (sbyte?)null, 1);

        Assert.AreEqual(15m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(20m, Library.Sum(Group, "test"));
    }

    #endregion

    #region Short Tests

    [TestMethod]
    public void SumShortTest()
    {
        Library.SetSum(Group, "test", (short)100);
        Library.SetSum(Group, "test", (short)200);
        Library.SetSum(Group, "test", (short)-50);
        Library.SetSum(Group, "test", (short?)null);

        Assert.AreEqual(250m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumShortParentTest()
    {
        Library.SetSum(Group, "test", (short)50, 1);
        Library.SetSum(Group, "test", (short)50, 1);
        Library.SetSum(Group, "test", (short)100);
        Library.SetSum(Group, "test", (short?)null, 1);

        Assert.AreEqual(100m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(100m, Library.Sum(Group, "test"));
    }

    #endregion

    #region UShort Tests

    [TestMethod]
    public void SumUShortTest()
    {
        Library.SetSum(Group, "test", (ushort)100);
        Library.SetSum(Group, "test", (ushort)200);
        Library.SetSum(Group, "test", (ushort)300);
        Library.SetSum(Group, "test", (ushort?)null);

        Assert.AreEqual(600m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumUShortParentTest()
    {
        Library.SetSum(Group, "test", (ushort)150, 1);
        Library.SetSum(Group, "test", (ushort)150, 1);
        Library.SetSum(Group, "test", (ushort)500);
        Library.SetSum(Group, "test", (ushort?)null, 1);

        Assert.AreEqual(300m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(500m, Library.Sum(Group, "test"));
    }

    #endregion

    #region UInt Tests

    [TestMethod]
    public void SumUIntTest()
    {
        Library.SetSum(Group, "test", 1000u);
        Library.SetSum(Group, "test", 2000u);
        Library.SetSum(Group, "test", 3000u);
        Library.SetSum(Group, "test", (uint?)null);

        Assert.AreEqual(6000m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumUIntParentTest()
    {
        Library.SetSum(Group, "test", 500u, 1);
        Library.SetSum(Group, "test", 500u, 1);
        Library.SetSum(Group, "test", 1000u);
        Library.SetSum(Group, "test", (uint?)null, 1);

        Assert.AreEqual(1000m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(1000m, Library.Sum(Group, "test"));
    }

    #endregion

    #region ULong Tests

    [TestMethod]
    public void SumULongTest()
    {
        Library.SetSum(Group, "test", 10000UL);
        Library.SetSum(Group, "test", 20000UL);
        Library.SetSum(Group, "test", 30000UL);
        Library.SetSum(Group, "test", (ulong?)null);

        Assert.AreEqual(60000m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumULongParentTest()
    {
        Library.SetSum(Group, "test", 5000UL, 1);
        Library.SetSum(Group, "test", 5000UL, 1);
        Library.SetSum(Group, "test", 15000UL);
        Library.SetSum(Group, "test", (ulong?)null, 1);

        Assert.AreEqual(10000m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(15000m, Library.Sum(Group, "test"));
    }

    #endregion

    #region Float Tests

    [TestMethod]
    public void SumFloatTest()
    {
        Library.SetSum(Group, "test", 1.5f);
        Library.SetSum(Group, "test", 2.5f);
        Library.SetSum(Group, "test", 3.0f);
        Library.SetSum(Group, "test", (float?)null);

        Assert.AreEqual(7m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumFloatParentTest()
    {
        Library.SetSum(Group, "test", 10.5f, 1);
        Library.SetSum(Group, "test", 10.5f, 1);
        Library.SetSum(Group, "test", 25.0f);
        Library.SetSum(Group, "test", (float?)null, 1);

        Assert.AreEqual(21m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(25m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumFloatWithNegativeTest()
    {
        Library.SetSum(Group, "test", 10.0f);
        Library.SetSum(Group, "test", -3.5f);
        Library.SetSum(Group, "test", 5.5f);

        Assert.AreEqual(12m, Library.Sum(Group, "test"));
    }

    #endregion

    #region Double Tests

    [TestMethod]
    public void SumDoubleTest()
    {
        Library.SetSum(Group, "test", 1.5d);
        Library.SetSum(Group, "test", 2.5d);
        Library.SetSum(Group, "test", 3.0d);
        Library.SetSum(Group, "test", (double?)null);

        Assert.AreEqual(7m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumDoubleParentTest()
    {
        Library.SetSum(Group, "test", 100.25d, 1);
        Library.SetSum(Group, "test", 100.25d, 1);
        Library.SetSum(Group, "test", 500.50d);
        Library.SetSum(Group, "test", (double?)null, 1);

        Assert.AreEqual(200.5m, Library.Sum(Group, "test", 1));
        Assert.AreEqual(500.5m, Library.Sum(Group, "test"));
    }

    [TestMethod]
    public void SumDoubleWithNegativeTest()
    {
        Library.SetSum(Group, "test", 100.0d);
        Library.SetSum(Group, "test", -25.5d);
        Library.SetSum(Group, "test", 50.5d);

        Assert.AreEqual(125m, Library.Sum(Group, "test"));
    }

    #endregion
}