using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class StDevTests : LibraryBaseBaseTests
    {

        [TestMethod]
        public void StdDevTest()
        {
            Library.SetStDev(Group, "test", 60000, 0);
            Library.SetStDev(Group, "test", 80000, 0);

            Assert.IsTrue(0.01m > (Library.StDev(Group, "test") - 14142.13m));
        }

        [TestMethod]
        public void StdDevTest_2()
        {
            Library.SetStDev(Group, "test", 5, 0);
            Library.SetStDev(Group, "test", 6, 0);
            Library.SetStDev(Group, "test", 8, 0);
            Library.SetStDev(Group, "test", 9, 0);

            Assert.IsTrue(0.001m > (Library.StDev(Group, "test") - 1.8257m));
        }
    }
}
