using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class AvgTests : LibraryBaseBaseTests
{
    #region Existing Tests

    [TestMethod]
    public void AvgIntTest()
    {
        Library.SetAvg(Group, "test", 5);
        Library.SetAvg(Group, "test", 4);
        Library.SetAvg(Group, "test", 6);
        Library.SetAvg(Group, "test", (int?)null);
        Library.SetAvg(Group, "test", -5);

        Assert.AreEqual(2.5m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgIntParentTest()
    {
        Library.SetAvg(Group, "test", 10, 1);
        Library.SetAvg(Group, "test", 10, 1);
        Library.SetAvg(Group, "test", 6);
        Library.SetAvg(Group, "test", (int?)null);

        Library.SetAvg(Group, "test", 10, 1);
        Library.SetAvg(Group, "test", -3);

        Assert.AreEqual(10m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(1.5m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgLongTest()
    {
        Library.SetAvg(Group, "test", 1L);
        Library.SetAvg(Group, "test", 4L);
        Library.SetAvg(Group, "test", 6L);
        Library.SetAvg(Group, "test", (long?)null);

        Library.SetAvg(Group, "test", -4);

        Assert.AreEqual(1.75m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgLongParentTest()
    {
        Library.SetAvg(Group, "test", 5L, 1);
        Library.SetAvg(Group, "test", 5L, 1);
        Library.SetAvg(Group, "test", 5L);
        Library.SetAvg(Group, "test", (long?)null, 1);

        Library.SetAvg(Group, "test", -1, 1);
        Library.SetAvg(Group, "test", -3);

        Assert.AreEqual(3m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(1m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgDecimalTest()
    {
        Library.SetAvg(Group, "test", 1m);
        Library.SetAvg(Group, "test", 2m);
        Library.SetAvg(Group, "test", 3m);
        Library.SetAvg(Group, "test", (decimal?)null);

        Library.SetAvg(Group, "test", -4);

        Assert.AreEqual(0.5m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void SumIncomeDecimalParentTest()
    {
        Library.SetAvg(Group, "test", 9m, 1);
        Library.SetAvg(Group, "test", 4m, 1);
        Library.SetAvg(Group, "test", 6m);
        Library.SetAvg(Group, "test", (decimal?)null, 1);

        Library.SetAvg(Group, "test", -1m, 1);
        Library.SetAvg(Group, "test", -2m);

        Assert.AreEqual(4m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(2m, Library.Avg(Group, "test"));
    }

    #endregion

    #region Byte Tests

    [TestMethod]
    public void AvgByteTest()
    {
        Library.SetAvg(Group, "test", (byte)10);
        Library.SetAvg(Group, "test", (byte)20);
        Library.SetAvg(Group, "test", (byte)30);
        Library.SetAvg(Group, "test", (byte?)null);

        Assert.AreEqual(20m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgByteParentTest()
    {
        Library.SetAvg(Group, "test", (byte)20, 1);
        Library.SetAvg(Group, "test", (byte)40, 1);
        Library.SetAvg(Group, "test", (byte)60);
        Library.SetAvg(Group, "test", (byte?)null, 1);

        Assert.AreEqual(30m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(60m, Library.Avg(Group, "test"));
    }

    #endregion

    #region SByte Tests

    [TestMethod]
    public void AvgSByteTest()
    {
        Library.SetAvg(Group, "test", (sbyte)10);
        Library.SetAvg(Group, "test", (sbyte)-10);
        Library.SetAvg(Group, "test", (sbyte)20);
        Library.SetAvg(Group, "test", (sbyte?)null);

        // (10 + (-10) + 20) / 3 = 20 / 3 = 6.666...
        var result = Library.Avg(Group, "test");
        Assert.IsTrue(result > 6.6m && result < 6.7m);
    }

    [TestMethod]
    public void AvgSByteParentTest()
    {
        Library.SetAvg(Group, "test", (sbyte)15, 1);
        Library.SetAvg(Group, "test", (sbyte)15, 1);
        Library.SetAvg(Group, "test", (sbyte)30);
        Library.SetAvg(Group, "test", (sbyte?)null, 1);

        Assert.AreEqual(15m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(30m, Library.Avg(Group, "test"));
    }

    #endregion

    #region Short Tests

    [TestMethod]
    public void AvgShortTest()
    {
        Library.SetAvg(Group, "test", (short)100);
        Library.SetAvg(Group, "test", (short)200);
        Library.SetAvg(Group, "test", (short)300);
        Library.SetAvg(Group, "test", (short?)null);

        Assert.AreEqual(200m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgShortParentTest()
    {
        Library.SetAvg(Group, "test", (short)50, 1);
        Library.SetAvg(Group, "test", (short)150, 1);
        Library.SetAvg(Group, "test", (short)500);
        Library.SetAvg(Group, "test", (short?)null, 1);

        Assert.AreEqual(100m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(500m, Library.Avg(Group, "test"));
    }

    #endregion

    #region UShort Tests

    [TestMethod]
    public void AvgUShortTest()
    {
        Library.SetAvg(Group, "test", (ushort)100);
        Library.SetAvg(Group, "test", (ushort)200);
        Library.SetAvg(Group, "test", (ushort)300);
        Library.SetAvg(Group, "test", (ushort?)null);

        Assert.AreEqual(200m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgUShortParentTest()
    {
        Library.SetAvg(Group, "test", (ushort)200, 1);
        Library.SetAvg(Group, "test", (ushort)200, 1);
        Library.SetAvg(Group, "test", (ushort)800);
        Library.SetAvg(Group, "test", (ushort?)null, 1);

        Assert.AreEqual(200m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(800m, Library.Avg(Group, "test"));
    }

    #endregion

    #region UInt Tests

    [TestMethod]
    public void AvgUIntTest()
    {
        Library.SetAvg(Group, "test", 1000u);
        Library.SetAvg(Group, "test", 2000u);
        Library.SetAvg(Group, "test", 3000u);
        Library.SetAvg(Group, "test", (uint?)null);

        Assert.AreEqual(2000m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgUIntParentTest()
    {
        Library.SetAvg(Group, "test", 500u, 1);
        Library.SetAvg(Group, "test", 500u, 1);
        Library.SetAvg(Group, "test", 2000u);
        Library.SetAvg(Group, "test", (uint?)null, 1);

        Assert.AreEqual(500m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(2000m, Library.Avg(Group, "test"));
    }

    #endregion

    #region ULong Tests

    [TestMethod]
    public void AvgULongTest()
    {
        Library.SetAvg(Group, "test", 10000UL);
        Library.SetAvg(Group, "test", 20000UL);
        Library.SetAvg(Group, "test", 30000UL);
        Library.SetAvg(Group, "test", (ulong?)null);

        Assert.AreEqual(20000m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgULongParentTest()
    {
        Library.SetAvg(Group, "test", 10000UL, 1);
        Library.SetAvg(Group, "test", 20000UL, 1);
        Library.SetAvg(Group, "test", 50000UL);
        Library.SetAvg(Group, "test", (ulong?)null, 1);

        Assert.AreEqual(15000m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(50000m, Library.Avg(Group, "test"));
    }

    #endregion

    #region Float Tests

    [TestMethod]
    public void AvgFloatTest()
    {
        Library.SetAvg(Group, "test", 1.5f);
        Library.SetAvg(Group, "test", 2.5f);
        Library.SetAvg(Group, "test", 5.0f);
        Library.SetAvg(Group, "test", (float?)null);

        Assert.AreEqual(3m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgFloatParentTest()
    {
        Library.SetAvg(Group, "test", 10.0f, 1);
        Library.SetAvg(Group, "test", 20.0f, 1);
        Library.SetAvg(Group, "test", 50.0f);
        Library.SetAvg(Group, "test", (float?)null, 1);

        Assert.AreEqual(15m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(50m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgFloatWithNegativeTest()
    {
        Library.SetAvg(Group, "test", 10.0f);
        Library.SetAvg(Group, "test", -5.0f);
        Library.SetAvg(Group, "test", 10.0f);

        // (10 + (-5) + 10) / 3 = 15 / 3 = 5
        Assert.AreEqual(5m, Library.Avg(Group, "test"));
    }

    #endregion

    #region Double Tests

    [TestMethod]
    public void AvgDoubleTest()
    {
        Library.SetAvg(Group, "test", 1.5d);
        Library.SetAvg(Group, "test", 2.5d);
        Library.SetAvg(Group, "test", 5.0d);
        Library.SetAvg(Group, "test", (double?)null);

        Assert.AreEqual(3m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgDoubleParentTest()
    {
        Library.SetAvg(Group, "test", 100.0d, 1);
        Library.SetAvg(Group, "test", 200.0d, 1);
        Library.SetAvg(Group, "test", 500.0d);
        Library.SetAvg(Group, "test", (double?)null, 1);

        Assert.AreEqual(150m, Library.Avg(Group, "test", 1));
        Assert.AreEqual(500m, Library.Avg(Group, "test"));
    }

    [TestMethod]
    public void AvgDoubleWithNegativeTest()
    {
        Library.SetAvg(Group, "test", 100.0d);
        Library.SetAvg(Group, "test", -50.0d);
        Library.SetAvg(Group, "test", 100.0d);

        // (100 + (-50) + 100) / 3 = 150 / 3 = 50
        Assert.AreEqual(50m, Library.Avg(Group, "test"));
    }

    #endregion
}