using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class SumIncomeTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void SumIncomeIntTest()
    {
        Library.SetSumIncome(Group, "test", 1);
        Library.SetSumIncome(Group, "test", 4);
        Library.SetSumIncome(Group, "test", 6);
        Library.SetSumIncome(Group, "test", (int?)null);

        Library.SetSumIncome(Group, "test", -4);

        Assert.AreEqual(11m, Library.SumIncome(Group, "test"));
    }

    [TestMethod]
    public void SumIncomeIntParentTest()
    {
        Library.SetSumIncome(Group, "test", 1, 1);
        Library.SetSumIncome(Group, "test", 4, 1);
        Library.SetSumIncome(Group, "test", 6);
        Library.SetSumIncome(Group, "test", (int?)null);

        Library.SetSumIncome(Group, "test", -4, 1);
        Library.SetSumIncome(Group, "test", -3);

        Assert.AreEqual(5, Library.SumIncome(Group, "test", 1));
        Assert.AreEqual(6, Library.SumIncome(Group, "test"));
    }

    [TestMethod]
    public void SumIncomeLongTest()
    {
        Library.SetSumIncome(Group, "test", 1L);
        Library.SetSumIncome(Group, "test", 4L);
        Library.SetSumIncome(Group, "test", 6L);
        Library.SetSumIncome(Group, "test", (long?)null);

        Library.SetSumIncome(Group, "test", -4);

        Assert.AreEqual(11m, Library.SumIncome(Group, "test"));
    }

    [TestMethod]
    public void SumIncomeLongParentTest()
    {
        Library.SetSumIncome(Group, "test", 1L, 1);
        Library.SetSumIncome(Group, "test", 4L, 1);
        Library.SetSumIncome(Group, "test", 6L);
        Library.SetSumIncome(Group, "test", (long?)null, 1);

        Library.SetSumIncome(Group, "test", -4, 1);
        Library.SetSumIncome(Group, "test", -3);

        Assert.AreEqual(5m, Library.SumIncome(Group, "test", 1));
        Assert.AreEqual(6m, Library.SumIncome(Group, "test"));
    }

    [TestMethod]
    public void SumIncomeDecimalTest()
    {
        Library.SetSumIncome(Group, "test", 1m);
        Library.SetSumIncome(Group, "test", 2m);
        Library.SetSumIncome(Group, "test", 3m);
        Library.SetSumIncome(Group, "test", (decimal?)null);

        Library.SetSumIncome(Group, "test", -4);

        Assert.AreEqual(6m, Library.SumIncome(Group, "test"));
    }

    [TestMethod]
    public void SumIncomeDecimalParentTest()
    {
        Library.SetSumIncome(Group, "test", 1m, 1);
        Library.SetSumIncome(Group, "test", 4m, 1);
        Library.SetSumIncome(Group, "test", 6m);
        Library.SetSumIncome(Group, "test", (decimal?)null, 1);

        Library.SetSumIncome(Group, "test", -4, 1);
        Library.SetSumIncome(Group, "test", -3);

        Assert.AreEqual(5m, Library.SumIncome(Group, "test", 1));
        Assert.AreEqual(6m, Library.SumIncome(Group, "test"));
    }
}
