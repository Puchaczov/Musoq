using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class AvgTests : LibraryBaseBaseTests
    {
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
    }
}
