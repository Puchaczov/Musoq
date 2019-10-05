using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class AggregateValuesTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void AggregateValuesIntTest()
        {
            Library.SetAggregateValues(Group, "test", 15);
            Library.SetAggregateValues(Group, "test", 20);
            Library.SetAggregateValues(Group, "test", 35);

            Assert.AreEqual("15,20,35", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesIntParentTest()
        {
            Library.SetAggregateValues(Group, "test", 15);
            Library.SetAggregateValues(Group, "test", 20, 1);
            Library.SetAggregateValues(Group, "test", 35, 1);

            Assert.AreEqual("20,35", Library.AggregateValues(Group, "test", 1));
            Assert.AreEqual("15", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesDecimalTest()
        {
            Library.SetAggregateValues(Group, "test", 15m);
            Library.SetAggregateValues(Group, "test", 20m);
            Library.SetAggregateValues(Group, "test", 35m);

            Assert.AreEqual("15,20,35", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesDecimalParentTest()
        {
            Library.SetAggregateValues(Group, "test", 15m);
            Library.SetAggregateValues(Group, "test", 20m, 1);
            Library.SetAggregateValues(Group, "test", 35m, 1);

            Assert.AreEqual("20,35", Library.AggregateValues(Group, "test", 1));
            Assert.AreEqual("15", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesLongTest()
        {
            Library.SetAggregateValues(Group, "test", 15L);
            Library.SetAggregateValues(Group, "test", 20L);
            Library.SetAggregateValues(Group, "test", 35L);

            Assert.AreEqual("15,20,35", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesLongParentTest()
        {
            Library.SetAggregateValues(Group, "test", 15L);
            Library.SetAggregateValues(Group, "test", 20L, 1);
            Library.SetAggregateValues(Group, "test", 35L, 1);

            Assert.AreEqual("20,35", Library.AggregateValues(Group, "test", 1));
            Assert.AreEqual("15", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesStringTest()
        {
            Library.SetAggregateValues(Group, "test", "15");
            Library.SetAggregateValues(Group, "test", "20");
            Library.SetAggregateValues(Group, "test", "35");

            Assert.AreEqual("15,20,35", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesStringParentTest()
        {
            Library.SetAggregateValues(Group, "test", "15");
            Library.SetAggregateValues(Group, "test", "20", 1);
            Library.SetAggregateValues(Group, "test", "35", 1);

            Assert.AreEqual("20,35", Library.AggregateValues(Group, "test", 1));
            Assert.AreEqual("15", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesDateTimeOffsetTest()
        {
            Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("01/01/2010"));
            Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("05/05/2015"));
            Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("03/02/2001"));

            var aggregated = Library.AggregateValues(Group, "test");

            Assert.AreEqual("01/01/2010 00:00:00 +01:00,05/05/2015 00:00:00 +02:00,02/03/2001 00:00:00 +01:00", aggregated);
        }

        [TestMethod]
        public void AggregateValuesDateTimeOffsetParentTest()
        {
            Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("01/01/2010"));
            Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("05/05/2015"), 1);
            Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("03/02/2001"), 1);

            Assert.AreEqual("05/05/2015 00:00:00 +02:00,02/03/2001 00:00:00 +01:00", Library.AggregateValues(Group, "test", 1));
            Assert.AreEqual("01/01/2010 00:00:00 +01:00", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesDateTimeTest()
        {
            Library.SetAggregateValues(Group, "test", DateTime.Parse("01/01/2010"));
            Library.SetAggregateValues(Group, "test", DateTime.Parse("05/05/2015"));
            Library.SetAggregateValues(Group, "test", DateTime.Parse("03/02/2001"));

            Assert.AreEqual("01/01/2010 00:00:00,05/05/2015 00:00:00,02/03/2001 00:00:00", Library.AggregateValues(Group, "test"));
        }

        [TestMethod]
        public void AggregateValuesDateTimeParentTest()
        {
            Library.SetAggregateValues(Group, "test", DateTime.Parse("01/01/2010"));
            Library.SetAggregateValues(Group, "test", DateTime.Parse("05/05/2015"), 1);
            Library.SetAggregateValues(Group, "test", DateTime.Parse("03/02/2001"), 1);

            Assert.AreEqual("05/05/2015 00:00:00,02/03/2001 00:00:00", Library.AggregateValues(Group, "test", 1));
            Assert.AreEqual("01/01/2010 00:00:00", Library.AggregateValues(Group, "test"));
        }
    }
}
