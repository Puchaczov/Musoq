using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class CountTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void CountIntTest()
    {
        Library.SetCount(Group, "test", 1);
        Library.SetCount(Group, "test", 4);
        Library.SetCount(Group, "test", 6);
        Library.SetCount(Group, "test", (int?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountIntParentTest()
    {
        Library.SetCount(Group, "test", 1, 1);
        Library.SetCount(Group, "test", 4, 1);
        Library.SetCount(Group, "test", 6);
        Library.SetCount(Group, "test", (int?)null);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountLongTest()
    {
        Library.SetCount(Group, "test", 1L);
        Library.SetCount(Group, "test", 4L);
        Library.SetCount(Group, "test", 6L);
        Library.SetCount(Group, "test", (long?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountLongParentTest()
    {
        Library.SetCount(Group, "test", 1L, 1);
        Library.SetCount(Group, "test", 4L, 1);
        Library.SetCount(Group, "test", 6L);
        Library.SetCount(Group, "test", (int?)null, 1);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountStringTest()
    {
        Library.SetCount(Group, "test", "1");
        Library.SetCount(Group, "test", "4");
        Library.SetCount(Group, "test", "5");
        Library.SetCount(Group, "test", (string?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountStringParentTest()
    {
        Library.SetCount(Group, "test", "1", 1);
        Library.SetCount(Group, "test", "4", 1);
        Library.SetCount(Group, "test", "6");
        Library.SetCount(Group, "test", (string?)null, 1);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountDecimalTest()
    {
        Library.SetCount(Group, "test", 1m);
        Library.SetCount(Group, "test", 2m);
        Library.SetCount(Group, "test", 3m);
        Library.SetCount(Group, "test", (decimal?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountDecimalParentTest()
    {
        Library.SetCount(Group, "test", 1m, 1);
        Library.SetCount(Group, "test", 4m, 1);
        Library.SetCount(Group, "test", 6m);
        Library.SetCount(Group, "test", (decimal?)null, 1);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountDateTimeOffsetTest()
    {
        Library.SetCount(Group, "test", DateTimeOffset.Parse("01/01/2010"));
        Library.SetCount(Group, "test", DateTimeOffset.Parse("01/01/2010"));
        Library.SetCount(Group, "test", DateTimeOffset.Parse("01/01/2010"));
        Library.SetCount(Group, "test", (DateTimeOffset?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountDateTimeOffsetParentTest()
    {
        Library.SetCount(Group, "test", DateTimeOffset.Parse("01/01/2010"), 1);
        Library.SetCount(Group, "test", DateTimeOffset.Parse("01/01/2010"), 1);
        Library.SetCount(Group, "test", DateTimeOffset.Parse("01/01/2010"));
        Library.SetCount(Group, "test", (DateTimeOffset?)null, 1);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountDateTimeTest()
    {
        Library.SetCount(Group, "test", DateTime.Parse("01/01/2010"));
        Library.SetCount(Group, "test", DateTime.Parse("01/01/2010"));
        Library.SetCount(Group, "test", DateTime.Parse("01/01/2010"));
        Library.SetCount(Group, "test", (DateTime?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountDateTimeParentTest()
    {
        Library.SetCount(Group, "test", DateTime.Parse("01/01/2010"), 1);
        Library.SetCount(Group, "test", DateTime.Parse("01/01/2010"), 1);
        Library.SetCount(Group, "test", DateTime.Parse("01/01/2010"));
        Library.SetCount(Group, "test", (DateTime?)null, 1);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountBooleanTest()
    {
        Library.SetCount(Group, "test", true);
        Library.SetCount(Group, "test", false);
        Library.SetCount(Group, "test", true);
        Library.SetCount(Group, "test", (bool?)null);

        Assert.AreEqual(3, Library.Count(Group, "test"));
    }

    [TestMethod]
    public void CountBooleanParentTest()
    {
        Library.SetCount(Group, "test", true, 1);
        Library.SetCount(Group, "test", false, 1);
        Library.SetCount(Group, "test", true);
        Library.SetCount(Group, "test", (bool?)null, 1);

        Assert.AreEqual(2, Library.Count(Group, "test", 1));
        Assert.AreEqual(1, Library.Count(Group, "test"));
    }
}
