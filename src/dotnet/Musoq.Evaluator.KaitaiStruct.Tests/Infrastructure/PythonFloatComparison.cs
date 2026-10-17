using System.Numerics;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal static class PythonFloatComparison
{
    internal static bool RoundsToZero(double difference, int places)
    {
        if (difference == 0)
            return true;
        if (!double.IsFinite(difference))
            return false;
        if (places > 323)
            return false;
        if (places <= -309)
            return true;
        var (numerator, denominator) = BinaryFraction(Math.Abs(difference));
        if (places >= 0)
            return 2 * numerator * BigInteger.Pow(10, places) <= denominator;
        return 2 * numerator <= denominator * BigInteger.Pow(10, -places);
    }

    private static (BigInteger Numerator, BigInteger Denominator) BinaryFraction(double value)
    {
        var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        var exponentBits = (int)((bits >> 52) & 0x7FF);
        var mantissa = bits & 0xFFFFFFFFFFFFF;
        var exponent = -1074;
        if (exponentBits != 0)
        {
            mantissa |= 1UL << 52;
            exponent = exponentBits - 1075;
        }
        var numerator = new BigInteger(mantissa);
        var denominator = BigInteger.One;
        if (exponent >= 0)
            numerator <<= exponent;
        else
            denominator <<= -exponent;
        return (numerator, denominator);
    }
}
