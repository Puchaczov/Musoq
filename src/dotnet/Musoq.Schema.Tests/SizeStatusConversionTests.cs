using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class SizeStatusConversionTests
{
    [TestMethod]
    public void IntegralBoundaries_ShouldRejectNullNegativeAndValuesBeyondIntRange()
    {
        Check<int>(null, false);
        Check<int>(-1, false);
        Check<int>(0, true, 0);
        Check<byte>(null, false);
        Check<byte>(byte.MaxValue, true, byte.MaxValue);
        Check<ushort>(0, true, 0);
        Check<ushort>(ushort.MaxValue, true, ushort.MaxValue);
        Check<int>(int.MaxValue, true, int.MaxValue);
        Check<uint>(uint.MaxValue, false);
        Check<long>((long)int.MaxValue + 1, false);
        Check<BigInteger>(new BigInteger(int.MaxValue), true, int.MaxValue);
        Check<BigInteger>(BigInteger.One << 2000, false);
    }

    [TestMethod]
    public void FloatingAndDecimalBoundaries_ShouldRejectRoundingSaturationAndNonIntegers()
    {
        Check<float>((float)int.MaxValue, false);
        Check<float>(2147483520f, true, 2147483520);
        Check<double>((double)int.MaxValue, true, int.MaxValue);
        Check<double>((double)int.MaxValue + 1, false);
        Check<double>(double.NaN, false);
        Check<double>(double.PositiveInfinity, false);
        Check<double>(1.5d, false);
        Check<decimal>((decimal)int.MaxValue, true, int.MaxValue);
        Check<decimal>((decimal)int.MaxValue + 1, false);
        Check<decimal>(0.5m, false);
    }

    private static void Check<T>(T? value, bool success, int expected = 0) where T : struct, INumberBase<T>
    {
        var result = new SizeParser().ConvertValue(value);
        Assert.AreEqual(success, result.Succeeded);
        if (success)
            Assert.AreEqual(expected, result.Value);
        else
            Assert.AreEqual(ParseErrorCode.InvalidSize, result.Failure!.ErrorCode);
    }

    private sealed class SizeParser : BytesInterpreterBase<int>
    {
        public override string SchemaName => "Sizes";
        public ParseAttempt<int> ConvertValue<T>(T? value) where T : struct, INumberBase<T> => ResolveSizeValueStatus(value);
        public override int InterpretAt(ReadOnlySpan<byte> data, int offset) => 0;
    }
}
