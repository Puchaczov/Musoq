using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DateTimeOffsetTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void WhenSingleValueAdded_ShouldPass()
    {
        Library.SetMinDateTimeOffset(Group, "min", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMaxDateTimeOffset(Group, "max", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var min = Library.MinDateTimeOffset(Group, "min", 0);
        var max = Library.MaxDateTimeOffset(Group, "max", 0);

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), min);
        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), max);
    }

    [TestMethod]
    public void WhenNullValueAdded_ShouldReturnDefaultMinMax()
    {
        Library.SetMinDateTimeOffset(Group, "min", null);
        Library.SetMaxDateTimeOffset(Group, "max", null);

        var min = Library.MinDateTimeOffset(Group, "min", 0);
        var max = Library.MaxDateTimeOffset(Group, "max", 0);

        Assert.AreEqual(default, min);
        Assert.AreEqual(default, max);
    }

    [TestMethod]
    public void WhenMultipleValuesAdded_ShouldReturnCorrectMinMax()
    {
        Library.SetMinDateTimeOffset(Group, "min", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMinDateTimeOffset(Group, "min", new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMaxDateTimeOffset(Group, "max", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMaxDateTimeOffset(Group, "max", new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var min = Library.MinDateTimeOffset(Group, "min", 0);
        var max = Library.MaxDateTimeOffset(Group, "max", 0);

        Assert.AreEqual(new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero), min);
        Assert.AreEqual(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), max);
    }

    [TestMethod]
    public void WhenInvalidDateString_ShouldReturnNull()
    {
        var result = Library.ToDateTimeOffset("invalid date");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenValidDateString_ShouldReturnDateTimeOffset()
    {
        var result = Library.ToDateTimeOffset("2020-01-01T00:00:00+00:00");

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), result);
    }

    [TestMethod]
    public void WhenValidDateStringWithCulture_ShouldReturnDateTimeOffset()
    {
        var result = Library.ToDateTimeOffset("01/01/2020 00:00:00", "en-US");

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).DateTime, result!.Value.DateTime);
    }

    [TestMethod]
    public void WhenFirstAddedNullThenValue_ShouldReturnCorrectMinMax()
    {
        Library.SetMinDateTimeOffset(Group, "min", null);
        Library.SetMinDateTimeOffset(Group, "min", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMaxDateTimeOffset(Group, "max", null);
        Library.SetMaxDateTimeOffset(Group, "max", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var min = Library.MinDateTimeOffset(Group, "min", 0);
        var max = Library.MaxDateTimeOffset(Group, "max", 0);

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), min);
        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), max);
    }

    [TestMethod]
    public void WhenFirstAddedValueThenNull_ShouldReturnCorrectMinMax()
    {
        Library.SetMinDateTimeOffset(Group, "min", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMinDateTimeOffset(Group, "min", null);
        Library.SetMaxDateTimeOffset(Group, "max", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Library.SetMaxDateTimeOffset(Group, "max", null);

        var min = Library.MinDateTimeOffset(Group, "min", 0);
        var max = Library.MaxDateTimeOffset(Group, "max", 0);

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), min);
        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), max);
    }

    [TestMethod]
    public void WhenTwoDateTimeOffsetsSubtracted_ShouldReturnTimeSpan()
    {
        var result = Library.SubtractDateTimeOffsets(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.AreEqual(TimeSpan.Zero, result);
    }

    [TestMethod]
    public void WhenOneDateTimeOffsetNull_ShouldReturnNull()
    {
        var result = Library.SubtractDateTimeOffsets(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenBothDateTimeOffsetsNull_ShouldReturnNull()
    {
        var result = Library.SubtractDateTimeOffsets(null, null);

        Assert.IsNull(result);
    }
}
