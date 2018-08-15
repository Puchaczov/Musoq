using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class HashFunctionsTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void Sha256Test()
        {
            Assert.AreEqual("9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08", Library.Sha256("test"));
        }

        [TestMethod]
        public void Sha256NullTest()
        {
            Assert.AreEqual(null, Library.Sha256(null));
        }

        [TestMethod]
        public void Sha512Test()
        {
            Assert.AreEqual("EE26B0DD4AF7E749AA1A8EE3C10AE9923F618980772E473F8819A5D4940E0DB27AC185F8A0E1D5F84F88BC887FD67B143732C304CC5FA9AD8E6F57F50028A8FF", Library.Sha512("test"));
        }

        [TestMethod]
        public void Sha512NullTest()
        {
            Assert.AreEqual(null, Library.Sha512(null));
        }

        [TestMethod]
        public void Md5Test()
        {
            Assert.AreEqual("098F6BCD4621D373CADE4E832627B4F6", Library.Md5("test"));
        }

        [TestMethod]
        public void Md5NullTest()
        {
            Assert.AreEqual(null, Library.Md5(null));
        }
    }
}
