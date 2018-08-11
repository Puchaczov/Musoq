using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class SumTests : LibraryBaseBaseTests
    {
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
    }
}
