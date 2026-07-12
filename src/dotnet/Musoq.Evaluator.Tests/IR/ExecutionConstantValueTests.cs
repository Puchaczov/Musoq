using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionConstantValueTests
{
    [TestMethod]
    public void FromClr_WhenPortableScalarsAreProvided_ShouldRoundTripCanonicalEncodings()
    {
        AssertRoundTrip(null, typeof(object), ExecutionConstantKind.Null);
        AssertRoundTrip(true, typeof(bool), ExecutionConstantKind.Boolean);
        AssertRoundTrip('x', typeof(char), ExecutionConstantKind.Character, 16);
        AssertRoundTrip(sbyte.MinValue, typeof(sbyte), ExecutionConstantKind.SignedInteger, 8);
        AssertRoundTrip(short.MinValue, typeof(short), ExecutionConstantKind.SignedInteger, 16);
        AssertRoundTrip(int.MinValue, typeof(int), ExecutionConstantKind.SignedInteger, 32);
        AssertRoundTrip(long.MinValue, typeof(long), ExecutionConstantKind.SignedInteger, 64);
        AssertRoundTrip(byte.MaxValue, typeof(byte), ExecutionConstantKind.UnsignedInteger, 8);
        AssertRoundTrip(ushort.MaxValue, typeof(ushort), ExecutionConstantKind.UnsignedInteger, 16);
        AssertRoundTrip(uint.MaxValue, typeof(uint), ExecutionConstantKind.UnsignedInteger, 32);
        AssertRoundTrip(ulong.MaxValue, typeof(ulong), ExecutionConstantKind.UnsignedInteger, 64);
        AssertRoundTrip(12.5m, typeof(decimal), ExecutionConstantKind.Decimal);
        AssertRoundTrip(TimeSpan.FromTicks(12345), typeof(TimeSpan), ExecutionConstantKind.TimeSpan);
    }

    [TestMethod]
    public void FromClr_WhenFloatingPointEdgesAreProvided_ShouldPreserveIeeeBits()
    {
        var single = ExecutionConstantValue.FromClr(-0.0f, ExecutionTypeRef.FromClr(typeof(float)));
        var nan = BitConverter.Int64BitsToDouble(unchecked((long)0x7ff8000000000042UL));
        var doubleValue = ExecutionConstantValue.FromClr(nan, ExecutionTypeRef.FromClr(typeof(double)));

        Assert.AreEqual(unchecked((uint)BitConverter.SingleToInt32Bits(-0.0f)), single.FloatingPointBits);
        Assert.AreEqual(0x7ff8000000000042UL, doubleValue.FloatingPointBits);
        Assert.AreEqual(BitConverter.SingleToInt32Bits(-0.0f), BitConverter.SingleToInt32Bits((float)single.ToClrValue()!));
        Assert.AreEqual(BitConverter.DoubleToInt64Bits(nan), BitConverter.DoubleToInt64Bits((double)doubleValue.ToClrValue()!));
    }

    [TestMethod]
    public void FromClr_WhenStructuredValuesAreProvided_ShouldUseDeterministicPayloads()
    {
        const string text = "A\U0001F600";
        var dateTime = new DateTime(638500000000000000L, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(638500000000000000L, TimeSpan.FromHours(2));
        var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

        var stringValue = ExecutionConstantValue.FromClr(text, ExecutionTypeRef.FromClr(typeof(string)));
        var dateTimeValue = ExecutionConstantValue.FromClr(dateTime, ExecutionTypeRef.FromClr(typeof(DateTime)));
        var offsetValue = ExecutionConstantValue.FromClr(dateTimeOffset, ExecutionTypeRef.FromClr(typeof(DateTimeOffset)));
        var guidValue = ExecutionConstantValue.FromClr(guid, ExecutionTypeRef.FromClr(typeof(Guid)));

        CollectionAssert.AreEqual(new ushort[] { 0x0041, 0xD83D, 0xDE00 }, stringValue.Utf16CodeUnits.ToArray());
        Assert.AreEqual(dateTime.Ticks, dateTimeValue.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, dateTimeValue.DateTimeKind);
        Assert.AreEqual(120, offsetValue.OffsetMinutes);
        Assert.AreEqual("00112233445566778899AABBCCDDEEFF", Convert.ToHexString(guidValue.GuidBytes.ToArray()));
        Assert.AreEqual(text, stringValue.ToClrValue());
        Assert.AreEqual(dateTime, dateTimeValue.ToClrValue());
        Assert.AreEqual(dateTimeOffset, offsetValue.ToClrValue());
        Assert.AreEqual(guid, guidValue.ToClrValue());
    }

    [TestMethod]
    public void FromClr_WhenEnumOrUnsupportedValueIsProvided_ShouldUseExplicitKinds()
    {
        var enumValue = ExecutionConstantValue.FromClr(DayOfWeek.Friday, ExecutionTypeRef.FromClr(typeof(DayOfWeek)));
        var unsupported = new UnsupportedConstant("value");
        var clrOnly = ExecutionConstantValue.FromClr(unsupported, ExecutionTypeRef.FromClr(typeof(UnsupportedConstant)));

        Assert.AreEqual(ExecutionConstantKind.Enum, enumValue.Kind);
        Assert.AreEqual(DayOfWeek.Friday, enumValue.ToClrValue());
        Assert.AreEqual(typeof(DayOfWeek), enumValue.EnumType?.ClrType);
        Assert.AreEqual(ExecutionConstantKind.ClrOnly, clrOnly.Kind);
        Assert.AreEqual(typeof(UnsupportedConstant), clrOnly.ClrOnlyType?.ClrType);
        Assert.AreSame(unsupported, clrOnly.ToClrValue());
    }

    [TestMethod]
    public void ConstantSet_WhenInputCollectionChanges_ShouldRemainImmutable()
    {
        var first = ExecutionConstantValue.FromClr(1, ExecutionTypeRef.FromClr(typeof(int)));
        var values = new[] { first };
        var set = new ExecutionConstantInSet(
            ExecutionTypeRef.FromClr(typeof(int)),
            values,
            ExecutionConstantInSetKind.Array);

        values[0] = ExecutionConstantValue.FromClr(2, ExecutionTypeRef.FromClr(typeof(int)));

        Assert.AreSame(first, set.Values[0]);
    }

    private static void AssertRoundTrip(
        object? expected,
        Type type,
        ExecutionConstantKind kind,
        int bitWidth = 0)
    {
        var value = ExecutionConstantValue.FromClr(expected, ExecutionTypeRef.FromClr(type));

        Assert.AreEqual(kind, value.Kind);
        Assert.AreEqual(bitWidth, value.BitWidth);
        Assert.AreEqual(expected, value.ToClrValue());
    }

    private sealed record UnsupportedConstant(string Value);
}
