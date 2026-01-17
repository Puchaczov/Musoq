using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class AggregateValuesTests : LibraryBaseBaseTests
{
    #region Existing Tests

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
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("01.01.2010 00:00:00 +01:00"));
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("05.05.2015 00:00:00 +02:00"));
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("29.01.2001 00:00:00 +01:00"));

        var aggregated = Library.AggregateValues(Group, "test");

        Assert.AreEqual("01.01.2010 00:00:00 +01:00,05.05.2015 00:00:00 +02:00,29.01.2001 00:00:00 +01:00", aggregated);
    }

    [TestMethod]
    public void AggregateValuesDateTimeOffsetParentTest()
    {
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("01.01.2010 00:00:00 +01:00"), "pl-PL");
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("05.05.2015 00:00:00 +02:00"), "pl-PL", 1);
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("03.02.2001 00:00:00 +01:00"), "pl-PL", 1);

        Assert.AreEqual("05.05.2015 00:00:00 +02:00,03.02.2001 00:00:00 +01:00", Library.AggregateValues(Group, "test", 1));
        Assert.AreEqual("01.01.2010 00:00:00 +01:00", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesDateTimeTest()
    {
        Library.SetAggregateValues(Group, "test", DateTime.Parse("01/01/2010"));
        Library.SetAggregateValues(Group, "test", DateTime.Parse("05/05/2015"));
        Library.SetAggregateValues(Group, "test", DateTime.Parse("03/02/2001"));

        Assert.AreEqual("01.01.2010 00:00:00,05.05.2015 00:00:00,03.02.2001 00:00:00", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesDateTimeParentTest()
    {
        Library.SetAggregateValues(Group, "test", DateTime.Parse("01/01/2010"));
        Library.SetAggregateValues(Group, "test", DateTime.Parse("05/05/2015"), 1);
        Library.SetAggregateValues(Group, "test", DateTime.Parse("03/02/2001"), 1);

        Assert.AreEqual("05.05.2015 00:00:00,03.02.2001 00:00:00", Library.AggregateValues(Group, "test", 1));
        Assert.AreEqual("01.01.2010 00:00:00", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Byte Tests

    [TestMethod]
    public void AggregateValuesByteTest()
    {
        Library.SetAggregateValues(Group, "test", (byte)10);
        Library.SetAggregateValues(Group, "test", (byte)20);
        Library.SetAggregateValues(Group, "test", (byte)30);

        Assert.AreEqual("10,20,30", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesByteNullTest()
    {
        Library.SetAggregateValues(Group, "test", (byte)10);
        Library.SetAggregateValues(Group, "test", (byte?)null);
        Library.SetAggregateValues(Group, "test", (byte)30);

        Assert.AreEqual("10,,30", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region SByte Tests

    [TestMethod]
    public void AggregateValuesSByteTest()
    {
        Library.SetAggregateValues(Group, "test", (sbyte)10);
        Library.SetAggregateValues(Group, "test", (sbyte)-5);
        Library.SetAggregateValues(Group, "test", (sbyte)15);

        Assert.AreEqual("10,-5,15", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesSByteNullTest()
    {
        Library.SetAggregateValues(Group, "test", (sbyte)10);
        Library.SetAggregateValues(Group, "test", (sbyte?)null);
        Library.SetAggregateValues(Group, "test", (sbyte)15);

        Assert.AreEqual("10,,15", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Short Tests

    [TestMethod]
    public void AggregateValuesShortTest()
    {
        Library.SetAggregateValues(Group, "test", (short)100);
        Library.SetAggregateValues(Group, "test", (short)200);
        Library.SetAggregateValues(Group, "test", (short)300);

        Assert.AreEqual("100,200,300", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesShortNullTest()
    {
        Library.SetAggregateValues(Group, "test", (short)100);
        Library.SetAggregateValues(Group, "test", (short?)null);
        Library.SetAggregateValues(Group, "test", (short)300);

        Assert.AreEqual("100,,300", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region UShort Tests

    [TestMethod]
    public void AggregateValuesUShortTest()
    {
        Library.SetAggregateValues(Group, "test", (ushort)100);
        Library.SetAggregateValues(Group, "test", (ushort)200);
        Library.SetAggregateValues(Group, "test", (ushort)300);

        Assert.AreEqual("100,200,300", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesUShortNullTest()
    {
        Library.SetAggregateValues(Group, "test", (ushort)100);
        Library.SetAggregateValues(Group, "test", (ushort?)null);
        Library.SetAggregateValues(Group, "test", (ushort)300);

        Assert.AreEqual("100,,300", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region UInt Tests

    [TestMethod]
    public void AggregateValuesUIntTest()
    {
        Library.SetAggregateValues(Group, "test", 1000u);
        Library.SetAggregateValues(Group, "test", 2000u);
        Library.SetAggregateValues(Group, "test", 3000u);

        Assert.AreEqual("1000,2000,3000", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesUIntNullTest()
    {
        Library.SetAggregateValues(Group, "test", 1000u);
        Library.SetAggregateValues(Group, "test", (uint?)null);
        Library.SetAggregateValues(Group, "test", 3000u);

        Assert.AreEqual("1000,,3000", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region ULong Tests

    [TestMethod]
    public void AggregateValuesULongTest()
    {
        Library.SetAggregateValues(Group, "test", 10000UL);
        Library.SetAggregateValues(Group, "test", 20000UL);
        Library.SetAggregateValues(Group, "test", 30000UL);

        Assert.AreEqual("10000,20000,30000", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesULongNullTest()
    {
        Library.SetAggregateValues(Group, "test", 10000UL);
        Library.SetAggregateValues(Group, "test", (ulong?)null);
        Library.SetAggregateValues(Group, "test", 30000UL);

        Assert.AreEqual("10000,,30000", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Float Tests

    [TestMethod]
    public void AggregateValuesFloatTest()
    {
        Library.SetAggregateValues(Group, "test", 1.5f);
        Library.SetAggregateValues(Group, "test", 2.5f);
        Library.SetAggregateValues(Group, "test", 3.5f);

        var result = Library.AggregateValues(Group, "test");
        Assert.IsTrue(result.Contains("1") && result.Contains("2") && result.Contains("3"));
    }

    [TestMethod]
    public void AggregateValuesFloatNullTest()
    {
        Library.SetAggregateValues(Group, "test", 1.5f);
        Library.SetAggregateValues(Group, "test", (float?)null);
        Library.SetAggregateValues(Group, "test", 3.5f);

        var result = Library.AggregateValues(Group, "test");
        Assert.Contains(",,", result); 
    }

    #endregion

    #region Double Tests

    [TestMethod]
    public void AggregateValuesDoubleTest()
    {
        Library.SetAggregateValues(Group, "test", 1.25);
        Library.SetAggregateValues(Group, "test", 2.25);
        Library.SetAggregateValues(Group, "test", 3.25);

        var result = Library.AggregateValues(Group, "test");
        Assert.IsTrue(result.Contains("1") && result.Contains("2") && result.Contains("3"));
    }

    [TestMethod]
    public void AggregateValuesDoubleNullTest()
    {
        Library.SetAggregateValues(Group, "test", 1.25);
        Library.SetAggregateValues(Group, "test", (double?)null);
        Library.SetAggregateValues(Group, "test", 3.25);

        var result = Library.AggregateValues(Group, "test");
        Assert.Contains(",,", result); 
    }

    #endregion

    #region Char Tests

    [TestMethod]
    public void AggregateValuesCharTest()
    {
        Library.SetAggregateValues(Group, "test", 'A');
        Library.SetAggregateValues(Group, "test", 'B');
        Library.SetAggregateValues(Group, "test", 'C');

        Assert.AreEqual("A,B,C", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesCharNullTest()
    {
        Library.SetAggregateValues(Group, "test", 'A');
        Library.SetAggregateValues(Group, "test", (char?)null);
        Library.SetAggregateValues(Group, "test", 'C');

        Assert.AreEqual("A,,C", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Bool Tests

    [TestMethod]
    public void AggregateValuesBoolTest()
    {
        Library.SetAggregateValues(Group, "test", true);
        Library.SetAggregateValues(Group, "test", false);
        Library.SetAggregateValues(Group, "test", true);

        Assert.AreEqual("True,False,True", Library.AggregateValues(Group, "test"));
    }

    [TestMethod]
    public void AggregateValuesBoolNullTest()
    {
        Library.SetAggregateValues(Group, "test", true);
        Library.SetAggregateValues(Group, "test", (bool?)null);
        Library.SetAggregateValues(Group, "test", false);

        Assert.AreEqual("True,,False", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region String Null Tests

    [TestMethod]
    public void AggregateValuesStringNullTest()
    {
        Library.SetAggregateValues(Group, "test", "hello");
        Library.SetAggregateValues(Group, "test", (string?)null);
        Library.SetAggregateValues(Group, "test", "world");

        Assert.AreEqual("hello,,world", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Int Null Tests

    [TestMethod]
    public void AggregateValuesIntNullTest()
    {
        Library.SetAggregateValues(Group, "test", 10);
        Library.SetAggregateValues(Group, "test", (int?)null);
        Library.SetAggregateValues(Group, "test", 30);

        Assert.AreEqual("10,,30", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Long Null Tests

    [TestMethod]
    public void AggregateValuesLongNullTest()
    {
        Library.SetAggregateValues(Group, "test", 100L);
        Library.SetAggregateValues(Group, "test", (long?)null);
        Library.SetAggregateValues(Group, "test", 300L);

        Assert.AreEqual("100,,300", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region Decimal Null Tests

    [TestMethod]
    public void AggregateValuesDecimalNullTest()
    {
        Library.SetAggregateValues(Group, "test", 10m);
        Library.SetAggregateValues(Group, "test", (decimal?)null);
        Library.SetAggregateValues(Group, "test", 30m);

        Assert.AreEqual("10,,30", Library.AggregateValues(Group, "test"));
    }

    #endregion

    #region DateTime Null Tests

    [TestMethod]
    public void AggregateValuesDateTimeNullTest()
    {
        Library.SetAggregateValues(Group, "test", DateTime.Parse("01/01/2020"));
        Library.SetAggregateValues(Group, "test", (DateTime?)null);
        Library.SetAggregateValues(Group, "test", DateTime.Parse("03/03/2020"));

        var result = Library.AggregateValues(Group, "test");
        Assert.Contains(",,", result); 
    }

    #endregion

    #region DateTimeOffset Null Tests

    [TestMethod]
    public void AggregateValuesDateTimeOffsetNullTest()
    {
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("01.01.2020 00:00:00 +01:00"));
        Library.SetAggregateValues(Group, "test", (DateTimeOffset?)null);
        Library.SetAggregateValues(Group, "test", DateTimeOffset.Parse("03.03.2020 00:00:00 +01:00"));

        var result = Library.AggregateValues(Group, "test");
        Assert.Contains(",,", result); 
    }

    #endregion
}