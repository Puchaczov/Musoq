using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Musoq.Evaluator.Tests;

[TestClass]
public class NumericLiteralTypesTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<sbyte>("1b", 1);
    }

    [TestMethod]
    public void WhenByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<byte>("1ub", 1);
    }

    [TestMethod]
    public void WhenShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("1s", 1);
    }

    [TestMethod]
    public void WhenUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ushort>("1us", 1);
    }

    [TestMethod]
    public void WhenIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i", 1);
    }

    [TestMethod]
    public void WhenUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui", 1);
    }

    [TestMethod]
    public void WhenLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l", 1);
    }

    [TestMethod]
    public void WhenULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ul", 1);
    }

    [TestMethod]
    public void WhenDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1d", 1);
    }

    [TestMethod]
    public void WhenDecimalWithDotUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0", 1);
    }

    [TestMethod]
    public void WhenDecimalWithDotAndSuffixUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d", 1);
    }

    [TestMethod]
    public void WhenNegativeSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<sbyte>("-1b", -1);
    }

    [TestMethod]
    public void WhenNegativeShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("-1s", -1);
    }

    [TestMethod]
    public void WhenNegativeIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("-1i", -1);
    }

    [TestMethod]
    public void WhenNegativeLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("-1l", -1);
    }

    [TestMethod]
    public void WhenNegativeDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("-1d", -1);
    }

    [TestMethod]
    public void WhenNegativeDecimalWithDotUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("-1.0", -1);
    }

    [TestMethod]
    public void WhenNegativeDecimalWithDotAndSuffixUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("-1.0d", -1);
    }

    [TestMethod]
    public void WhenLargeSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<sbyte>("127b", 127);
    }

    [TestMethod]
    public void WhenLargeByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<byte>("255ub", 255);
    }

    [TestMethod]
    public void WhenLargeShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("32767s", 32767);
    }

    [TestMethod]
    public void WhenLargeUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ushort>("65535us", 65535);
    }

    [TestMethod]
    public void WhenLargeIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("2147483647i", 2147483647);
    }

    [TestMethod]
    public void WhenLargeUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("4294967295ui", 4294967295);
    }

    [TestMethod]
    public void WhenLargeLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("9223372036854775807l", 9223372036854775807);
    }

    [TestMethod]
    public void WhenLargeULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("18446744073709551615ul", 18446744073709551615);
    }

    [TestMethod]
    public void WhenLargeDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("79228162514264337593543950335d", 79228162514264337593543950335m);
    }

    [TestMethod]
    public void WhenLargeDecimalWithDotUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("79228162514264337593543950334.0", 79228162514264337593543950334.0m);
    }

    [TestMethod]
    public void WhenLargeDecimalWithDotAndSuffixUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("79228162514264337593543950334.0d", 79228162514264337593543950334.0m);
    }

    [TestMethod]
    public void WhenLargeNegativeSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<sbyte>("-128b", -128);
    }

    [TestMethod]
    public void WhenLargeNegativeShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("-32768s", -32768);
    }

    [TestMethod]
    public void WhenLargeNegativeIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("-2147483648i", -2147483648);
    }

    [TestMethod]
    public void WhenLargeNegativeLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("-9223372036854775808l", -9223372036854775808);
    }

    [TestMethod]
    public void WhenLargeNegativeDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("-79228162514264337593543950335d", -79228162514264337593543950335m);
    }

    [TestMethod]
    public void WhenLargeNegativeDecimalWithDotUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("-79228162514264337593543950334.0", -79228162514264337593543950334.0m);
    }

    [TestMethod]
    public void WhenLargeNegativeDecimalWithDotAndSuffixUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("-79228162514264337593543950334.0d", -79228162514264337593543950334.0m);
    }
}
