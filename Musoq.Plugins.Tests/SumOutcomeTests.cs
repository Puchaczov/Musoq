using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class SumOutcomeTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void SumOutcomeIntTest()
    {
        Library.SetSumOutcome(Group, "test", -1);
        Library.SetSumOutcome(Group, "test", -4);
        Library.SetSumOutcome(Group, "test", -6);
        Library.SetSumOutcome(Group, "test", (int?)null);

        Library.SetSumOutcome(Group, "test", 4);

        Assert.AreEqual(-11m, Library.SumOutcome(Group, "test"));
    }

    [TestMethod]
    public void SumOutcomeIntParentTest()
    {
        Library.SetSumOutcome(Group, "test", -1, 1);
        Library.SetSumOutcome(Group, "test", -4, 1);
        Library.SetSumOutcome(Group, "test", -6);
        Library.SetSumOutcome(Group, "test", (int?)null);

        Library.SetSumOutcome(Group, "test", 4, 1);
        Library.SetSumOutcome(Group, "test", 3);

        Assert.AreEqual(-5, Library.SumOutcome(Group, "test", 1));
        Assert.AreEqual(-6, Library.SumOutcome(Group, "test"));
    }

    [TestMethod]
    public void SumOutcomeLongTest()
    {
        Library.SetSumOutcome(Group, "test", -1L);
        Library.SetSumOutcome(Group, "test", -4L);
        Library.SetSumOutcome(Group, "test", -6L);
        Library.SetSumOutcome(Group, "test", (long?)null);

        Library.SetSumOutcome(Group, "test", 4);

        Assert.AreEqual(-11m, Library.SumOutcome(Group, "test"));
    }

    [TestMethod]
    public void SumOutcomeLongParentTest()
    {
        Library.SetSumOutcome(Group, "test", -1L, 1);
        Library.SetSumOutcome(Group, "test", -4L, 1);
        Library.SetSumOutcome(Group, "test", -6L);
        Library.SetSumOutcome(Group, "test", (long?)null, 1);

        Library.SetSumOutcome(Group, "test", 4, 1);
        Library.SetSumOutcome(Group, "test", 3);

        Assert.AreEqual(-5m, Library.SumOutcome(Group, "test", 1));
        Assert.AreEqual(-6m, Library.SumOutcome(Group, "test"));
    }

    [TestMethod]
    public void SumOutcomeDecimalTest()
    {
        Library.SetSumOutcome(Group, "test", -1m);
        Library.SetSumOutcome(Group, "test", -2m);
        Library.SetSumOutcome(Group, "test", -3m);
        Library.SetSumOutcome(Group, "test", (decimal?)null);

        Library.SetSumOutcome(Group, "test", 4);

        Assert.AreEqual(-6m, Library.SumOutcome(Group, "test"));
    }

    [TestMethod]
    public void SumOutcomeDecimalParentTest()
    {
        Library.SetSumOutcome(Group, "test", -1m, 1);
        Library.SetSumOutcome(Group, "test", -4m, 1);
        Library.SetSumOutcome(Group, "test", -6m);
        Library.SetSumOutcome(Group, "test", (decimal?)null, 1);

        Library.SetSumOutcome(Group, "test", 4, 1);
        Library.SetSumOutcome(Group, "test", 3);

        Assert.AreEqual(-5m, Library.SumOutcome(Group, "test", 1));
        Assert.AreEqual(-6m, Library.SumOutcome(Group, "test"));
    }
}