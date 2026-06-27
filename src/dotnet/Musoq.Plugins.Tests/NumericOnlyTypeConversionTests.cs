using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for NumericOnly type converters that reject strings and only accept boxed numeric types.
///     Used for arithmetic operations on System.Object columns.
/// </summary>
[TestClass]
public class NumericOnlyTypeConversionTests
{
    private readonly LibraryBase _library = new();

    public static IEnumerable<object?[]> Int32Cases()
    {
        yield return [null, null];
        yield return [42, 42];
        yield return [(byte)200, 200];
        yield return [(sbyte)-100, -100];
        yield return [(short)30000, 30000];
        yield return [(ushort)60000, 60000];
        yield return [(uint)1000, 1000];
        yield return [3000000000, null];
        yield return [1000L, 1000];
        yield return [10000000000L, null];
        yield return [-10000000000L, null];
        yield return [(ulong)1000, 1000];
        yield return [(ulong)10000000000, null];
        yield return [42.0f, 42];
        yield return [float.NaN, null];
        yield return [float.PositiveInfinity, null];
        yield return [float.NegativeInfinity, null];
        yield return [42.5f, null];
        yield return [42.0, 42];
        yield return [double.NaN, null];
        yield return [double.PositiveInfinity, null];
        yield return [42.5, null];
        yield return [42m, 42];
        yield return [42.5m, null];
        yield return ["42", null];
        yield return [true, null];
    }

    [TestMethod]
    [DynamicData(nameof(Int32Cases))]
    public void TryConvertToInt32NumericOnly_ReturnsExpected(object? value, int? expected)
    {
        Assert.AreEqual(expected, _library.TryConvertToInt32NumericOnly(value));
    }

    public static IEnumerable<object?[]> Int64Cases()
    {
        yield return [null, null];
        yield return [42L, 42L];
        yield return [(byte)200, 200L];
        yield return [(sbyte)-100, -100L];
        yield return [(short)30000, 30000L];
        yield return [(ushort)60000, 60000L];
        yield return [42, 42L];
        yield return [3000000000, 3000000000L];
        yield return [(ulong)1000, 1000L];
        yield return [ulong.MaxValue, null];
        yield return [42.0f, 42L];
        yield return [float.NaN, null];
        yield return [float.PositiveInfinity, null];
        yield return [42.5f, null];
        yield return [42.0, 42L];
        yield return [double.NaN, null];
        yield return [double.PositiveInfinity, null];
        yield return [42.5, null];
        yield return [42m, 42L];
        yield return [42.5m, null];
        yield return ["42", null];
        yield return [true, null];
    }

    [TestMethod]
    [DynamicData(nameof(Int64Cases))]
    public void TryConvertToInt64NumericOnly_ReturnsExpected(object? value, long? expected)
    {
        Assert.AreEqual(expected, _library.TryConvertToInt64NumericOnly(value));
    }

    public static IEnumerable<object?[]> DecimalCases()
    {
        yield return [null, null];
        yield return [42.5m, 42.5m];
        yield return [(byte)200, 200m];
        yield return [(sbyte)-100, -100m];
        yield return [(short)30000, 30000m];
        yield return [(ushort)60000, 60000m];
        yield return [42, 42m];
        yield return [3000000000, 3000000000m];
        yield return [9000000000L, 9000000000m];
        yield return [(ulong)9000000000, 9000000000m];
        yield return [float.NaN, null];
        yield return [float.PositiveInfinity, null];
        yield return [42.5, 42.5m];
        yield return [double.NaN, null];
        yield return [double.PositiveInfinity, null];
        yield return ["42", null];
        yield return [true, null];
    }

    [TestMethod]
    [DynamicData(nameof(DecimalCases))]
    public void TryConvertToDecimalNumericOnly_ReturnsExpected(object? value, decimal? expected)
    {
        Assert.AreEqual(expected, _library.TryConvertToDecimalNumericOnly(value));
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Float_Converts()
    {
        Assert.IsNotNull(_library.TryConvertToDecimalNumericOnly(42.5f));
    }

    public static IEnumerable<object?[]> DoubleCases()
    {
        yield return [null, null];
        yield return [42.5, 42.5];
        yield return [double.NaN, null];
        yield return [double.PositiveInfinity, null];
        yield return [double.NegativeInfinity, null];
        yield return [float.NaN, null];
        yield return [float.PositiveInfinity, null];
        yield return [42, 42.0];
        yield return [9000000000L, 9000000000.0];
        yield return [42.5m, 42.5];
        yield return ["42", null];
        yield return [true, 1.0];
    }

    [TestMethod]
    [DynamicData(nameof(DoubleCases))]
    public void TryConvertToDoubleNumericOnly_ReturnsExpected(object? value, double? expected)
    {
        Assert.AreEqual(expected, _library.TryConvertToDoubleNumericOnly(value));
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Float_Valid_Converts()
    {
        Assert.IsNotNull(_library.TryConvertToDoubleNumericOnly(42.5f));
    }

    public static IEnumerable<object?[]> NumericCases()
    {
        yield return [null, null];
        yield return [42, 42m];
        yield return [9000000000L, 9000000000m];
        yield return [42.5m, 42.5m];
        yield return [42.5, 42.5m];
        yield return ["42", null];
    }

    [TestMethod]
    [DynamicData(nameof(NumericCases))]
    public void TryConvertNumericOnly_ReturnsExpected(object? value, decimal? expected)
    {
        Assert.AreEqual(expected, _library.TryConvertNumericOnly(value));
    }

    [TestMethod]
    [DataRow(42.5f, DisplayName = "Float")]
    [DataRow(10000000000000000000, DisplayName = "Large ulong")]
    [DataRow(1e20, DisplayName = "Large double")]
    public void TryConvertNumericOnly_LargeOrFloatValue_Converts(object value)
    {
        Assert.IsNotNull(_library.TryConvertNumericOnly(value));
    }
}
