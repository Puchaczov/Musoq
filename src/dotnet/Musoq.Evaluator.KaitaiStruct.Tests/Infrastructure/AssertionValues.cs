using System.Numerics;
using System.Text.Json;
using Musoq.Evaluator.Tables;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal static class AssertionValues
{
    internal static object? Read(JsonElement assertion, Row row, int fallbackColumn)
    {
        if (!assertion.TryGetProperty("resultColumns", out var columns))
            return row.ReadValue(assertion.TryGetProperty("resultColumn", out var column) ? column.GetInt32() : fallbackColumn);
        var length = row.ReadValue(columns.GetProperty("length").GetInt32());
        Assert.IsTrue(length is byte or sbyte or short or ushort or int or uint or long or ulong);
        var elements = columns.GetProperty("elements").EnumerateArray().Select(element => element.GetInt32()).ToArray();
        Assert.AreEqual(new BigInteger(elements.Length), BigInteger.Parse(Convert.ToString(length, System.Globalization.CultureInfo.InvariantCulture)!));
        return elements.Select(row.ReadValue).ToArray();
    }

    internal static int ColumnCount(JsonElement[] assertions) => Math.Max(1, assertions.Sum(assertion =>
        assertion.TryGetProperty("resultColumns", out var columns) ? columns.GetProperty("elements").GetArrayLength() + 1 : 1));
}
