using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class MaxTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void MaxIntTest()
    {
        Library.SetMax(Group, "test", 5);
        Library.SetMax(Group, "test", 4);
        Library.SetMax(Group, "test", 6);
        Library.SetMax(Group, "test", (int?)null);
        Library.SetMax(Group, "test", -5);

        Assert.AreEqual(6m, Library.Max(Group, "test"));
    }

    [TestMethod]
    public void MaxIntParentTest()
    {
        Library.SetMax(Group, "test", 10, 1);
        Library.SetMax(Group, "test", 10, 1);
        Library.SetMax(Group, "test", 6);
        Library.SetMax(Group, "test", (int?)null);

        Library.SetMax(Group, "test", 10, 1);
        Library.SetMax(Group, "test", -3);

        Assert.AreEqual(10m, Library.Max(Group, "test", 1));
        Assert.AreEqual(6m, Library.Max(Group, "test"));
    }

    [TestMethod]
    public void MaxLongTest()
    {
        Library.SetMax(Group, "test", 1L);
        Library.SetMax(Group, "test", 4L);
        Library.SetMax(Group, "test", 6L);
        Library.SetMax(Group, "test", (long?)null);

        Library.SetMax(Group, "test", -4);

        Assert.AreEqual(6m, Library.Max(Group, "test"));
    }

    [TestMethod]
    public void MaxLongParentTest()
    {
        Library.SetMax(Group, "test", 5L, 1);
        Library.SetMax(Group, "test", 5L, 1);
        Library.SetMax(Group, "test", 5L);
        Library.SetMax(Group, "test", (long?)null, 1);

        Library.SetMax(Group, "test", -1, 1);
        Library.SetMax(Group, "test", -3);

        Assert.AreEqual(5m, Library.Max(Group, "test", 1));
        Assert.AreEqual(5m, Library.Max(Group, "test"));
    }

    [TestMethod]
    public void MaxDecimalTest()
    {
        Library.SetMax(Group, "test", 1m);
        Library.SetMax(Group, "test", 2m);
        Library.SetMax(Group, "test", 3m);
        Library.SetMax(Group, "test", (decimal?)null);

        Library.SetMax(Group, "test", -4m);

        Assert.AreEqual(3m, Library.Max(Group, "test"));
    }

    [TestMethod]
    public void MaxDecimalParentTest()
    {
        Library.SetMax(Group, "test", 9m, 1);
        Library.SetMax(Group, "test", 4m, 1);
        Library.SetMax(Group, "test", 6m);
        Library.SetMax(Group, "test", (decimal?)null, 1);

        Library.SetMax(Group, "test", -1m, 1);
        Library.SetMax(Group, "test", -2m);

        Assert.AreEqual(9m, Library.Max(Group, "test", 1));
        Assert.AreEqual(6m, Library.Max(Group, "test"));
    }
}
