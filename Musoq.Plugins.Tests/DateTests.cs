using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class DateTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void ExtractFromDateTest()
        {
            Assert.AreEqual(2, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "month"));
            Assert.AreEqual(1, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "day"));
            Assert.AreEqual(2001, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "year"));
        }

        [ExpectedException(typeof(NotSupportedException))]
        [TestMethod]
        public void ExtracFromDateWrongDateTest()
        {
            Library.ExtractFromDate("error", "month");
        }

        [ExpectedException(typeof(NotSupportedException))]
        [TestMethod]
        public void ExtracFromDateWrongPartOfDateTest()
        {
            Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "error");
        }

        [TestMethod]
        public void ExtractFromDateWithCultureInfoTest()
        {
            Assert.AreEqual(2, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "pl-PL", "month"));
            Assert.AreEqual(1, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "pl-PL", "day"));
            Assert.AreEqual(2001, Library.ExtractFromDate("01/02/2001 00:00:00 +00:00", "pl-PL", "year"));
        }

        [TestMethod]
        public void YearTest()
        {
            Assert.AreEqual(2012, Library.Year(DateTimeOffset.Parse("01/01/2012 00:00:00 +00:00")));
            Assert.AreEqual(null, Library.Year(null));
        }

        [TestMethod]
        public void MonthTest()
        {
            Assert.AreEqual(2, Library.Month(DateTimeOffset.Parse("01/02/2012 00:00:00 +00:00")));
            Assert.AreEqual(null, Library.Month(null));
        }

        [TestMethod]
        public void DayTest()
        {
            Assert.AreEqual(1, Library.Day(DateTimeOffset.Parse("01/02/2012 00:00:00 +00:00")));
            Assert.AreEqual(null, Library.Day(null));
        }
    }
}
