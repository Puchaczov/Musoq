using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class MinTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void MinIntTest()
        {
            Library.SetMin(Group, "test", 5);
            Library.SetMin(Group, "test", 4);
            Library.SetMin(Group, "test", 6);
            Library.SetMin(Group, "test", (int?)null);
            Library.SetMin(Group, "test", -5);

            Assert.AreEqual(-5m, Library.Min(Group, "test"));
        }

        [TestMethod]
        public void MinIntParentTest()
        {
            Library.SetMin(Group, "test", 10, 1);
            Library.SetMin(Group, "test", 10, 1);
            Library.SetMin(Group, "test", 6);
            Library.SetMin(Group, "test", (int?)null);

            Library.SetMin(Group, "test", 10, 1);
            Library.SetMin(Group, "test", -3);

            Assert.AreEqual(10m, Library.Min(Group, "test", 1));
            Assert.AreEqual(-3m, Library.Min(Group, "test"));
        }

        [TestMethod]
        public void MinLongTest()
        {
            Library.SetMin(Group, "test", 1L);
            Library.SetMin(Group, "test", 4L);
            Library.SetMin(Group, "test", 6L);
            Library.SetMin(Group, "test", (long?)null);

            Library.SetMin(Group, "test", -4);

            Assert.AreEqual(-4m, Library.Min(Group, "test"));
        }

        [TestMethod]
        public void MinLongParentTest()
        {
            Library.SetMin(Group, "test", 5L, 1);
            Library.SetMin(Group, "test", 5L, 1);
            Library.SetMin(Group, "test", 5L);
            Library.SetMin(Group, "test", (long?)null, 1);

            Library.SetMin(Group, "test", -1, 1);
            Library.SetMin(Group, "test", -3);

            Assert.AreEqual(-1m, Library.Min(Group, "test", 1));
            Assert.AreEqual(-3m, Library.Min(Group, "test"));
        }

        [TestMethod]
        public void MinDecimalTest()
        {
            Library.SetMin(Group, "test", 1m);
            Library.SetMin(Group, "test", 2m);
            Library.SetMin(Group, "test", 3m);
            Library.SetMin(Group, "test", (decimal?)null);

            Library.SetMin(Group, "test", -4m);

            Assert.AreEqual(-4m, Library.Min(Group, "test"));
        }

        [TestMethod]
        public void MinDecimalParentTest()
        {
            Library.SetMin(Group, "test", 9m, 1);
            Library.SetMin(Group, "test", 4m, 1);
            Library.SetMin(Group, "test", 6m);
            Library.SetMin(Group, "test", (decimal?)null, 1);

            Library.SetMin(Group, "test", -1m, 1);
            Library.SetMin(Group, "test", -2m);

            Assert.AreEqual(-1m, Library.Min(Group, "test", 1));
            Assert.AreEqual(-2m, Library.Min(Group, "test"));
        }
    }
}
