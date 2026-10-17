using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal static class AssertionComparer
{
    public static void Compare(JsonElement assertion, object? actual, string context)
    {
        var expected = assertion.GetProperty("expected");
        switch (expected.GetProperty("kind").GetString())
        {
            case "null":
                Assert.IsNull(actual, context);
                return;
            case "integer":
                Assert.IsTrue(actual is byte or sbyte or short or ushort or int or uint or long or ulong,
                    $"{context}: expected integral result, received {actual?.GetType()}");
                Assert.AreEqual(BigInteger.Parse(expected.GetProperty("value").GetString()!, CultureInfo.InvariantCulture),
                    BigInteger.Parse(Convert.ToString(actual, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture), context);
                return;
            case "float":
                CompareFloat(assertion, expected, actual, context);
                return;
            case "boolean":
                Assert.AreEqual(expected.GetProperty("value").GetBoolean(), actual, context);
                return;
            case "string":
                Assert.AreEqual(expected.GetProperty("value").GetString(), actual, context);
                return;
            case "bytes":
                Assert.IsInstanceOfType<byte[]>(actual, context);
                CollectionAssert.AreEqual(Convert.FromHexString(expected.GetProperty("value").GetString()!), (byte[])actual, context);
                return;
            case "array":
                Assert.IsInstanceOfType<IEnumerable>(actual, context);
                var values = ((IEnumerable)actual).Cast<object?>().ToArray();
                var elements = expected.GetProperty("value").EnumerateArray().ToArray();
                Assert.AreEqual(elements.Length, values.Length, context);
                for (var index = 0; index < values.Length; index++)
                {
                    using var nested = JsonDocument.Parse("{\"expected\":" + elements[index].GetRawText() + "}");
                    Compare(nested.RootElement, values[index], $"{context}[{index}]");
                }
                return;
            default:
                throw new InvalidOperationException($"Unknown expectation kind: {expected}");
        }
    }

    private static void CompareFloat(JsonElement assertion, JsonElement expected, object? actual, string context)
    {
        Assert.IsTrue(actual is float or double or decimal, $"{context}: expected floating result, received {actual?.GetType()}");
        var value = ParseFloat(expected.GetProperty("value").GetString()!);
        var result = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
        if (assertion.TryGetProperty("delta", out var delta))
        {
            var tolerance = double.Parse(delta.GetString()!, CultureInfo.InvariantCulture);
            Assert.IsTrue(double.IsFinite(result) && double.IsFinite(value) &&
                          Math.Abs(value - result) <= tolerance, context);
            return;
        }
        if (result == value)
            return;
        if (assertion.TryGetProperty("places", out var places))
        {
            Assert.IsTrue(PythonFloatComparison.RoundsToZero(Math.Abs(value - result), places.GetInt32()), context);
            return;
        }
        Assert.IsTrue(value == result, context);
    }

    private static double ParseFloat(string value) => value switch
    {
        "inf" => double.PositiveInfinity,
        "-inf" => double.NegativeInfinity,
        "nan" => double.NaN,
        _ => double.Parse(value, CultureInfo.InvariantCulture)
    };
}
