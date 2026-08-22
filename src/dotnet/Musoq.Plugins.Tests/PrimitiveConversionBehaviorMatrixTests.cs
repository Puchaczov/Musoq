using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class PrimitiveConversionBehaviorMatrixTests : PluginsTestBase
{
    private static readonly Type[] NumericSourceTypes =
    [
        typeof(string),
        typeof(byte?),
        typeof(sbyte?),
        typeof(short?),
        typeof(ushort?),
        typeof(int?),
        typeof(uint?),
        typeof(long?),
        typeof(ulong?),
        typeof(float?),
        typeof(double?),
        typeof(decimal?),
        typeof(bool?),
        typeof(char?),
        typeof(object)
    ];

    private static readonly IReadOnlyDictionary<string, Type> NumericTargets =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["ToBoolean"] = typeof(bool),
            ["ToByte"] = typeof(byte),
            ["ToSByte"] = typeof(sbyte),
            ["ToInt16"] = typeof(short),
            ["ToUInt16"] = typeof(ushort),
            ["ToInt32"] = typeof(int),
            ["ToUInt32"] = typeof(uint),
            ["ToInt64"] = typeof(long),
            ["ToUInt64"] = typeof(ulong),
            ["ToSingle"] = typeof(float),
            ["ToDouble"] = typeof(double),
            ["ToDecimal"] = typeof(decimal)
        };

    public static IEnumerable<object?[]> NumericCases
    {
        get
        {
            foreach (var (target, resultType) in NumericTargets)
            foreach (var sourceType in NumericSourceTypes)
            {
                var input = CreateNumericInput(target, sourceType);
                yield return
                [
                    $"{target}_{FriendlyName(sourceType)}_Valid",
                    target,
                    sourceType,
                    input,
                    ExpectedNumericValue(target, resultType, sourceType, input)
                ];
                yield return
                [
                    $"{target}_{FriendlyName(sourceType)}_Null",
                    target,
                    sourceType,
                    null,
                    null
                ];
            }
        }
    }

    public static IEnumerable<object?[]> NonNumericCases
    {
        get
        {
            foreach (var sourceType in NumericSourceTypes)
            {
                var input = CreateCharInput(sourceType);
                yield return
                [
                    $"ToChar_{FriendlyName(sourceType)}_Valid",
                    "ToChar",
                    sourceType,
                    input,
                    ExpectedCharValue(sourceType, input)
                ];
                yield return [$"ToChar_{FriendlyName(sourceType)}_Null", "ToChar", sourceType, null, null];
            }

            foreach (var (sourceType, input, expected) in ToStringInputs())
            {
                yield return [$"ToString_{FriendlyName(sourceType)}_Valid", "ToString", sourceType, input, expected];
                yield return [$"ToString_{FriendlyName(sourceType)}_Null", "ToString", sourceType, null, null];
            }

            var dateTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var dateTimeOffset = new DateTimeOffset(dateTime);
            var timeSpan = new TimeSpan(1, 2, 3);
            var guid = Guid.Parse("5f38db3f-6a65-4ab6-8ad4-c14d8de21a1c");

            yield return ["ToDateTime_String_Valid", "ToDateTime", typeof(string), "2024-01-02T03:04:05", dateTime.DateTimeWithoutKind()];
            yield return ["ToDateTime_DateTime_Valid", "ToDateTime", typeof(DateTime?), dateTime, dateTime];
            yield return ["ToDateTime_DateTimeOffset_Valid", "ToDateTime", typeof(DateTimeOffset?), dateTimeOffset, dateTimeOffset.DateTime];
            yield return ["ToDateTime_Object_Valid", "ToDateTime", typeof(object), dateTimeOffset, dateTimeOffset.DateTime];

            yield return ["ToDateTimeOffset_String_Valid", "ToDateTimeOffset", typeof(string), "2024-01-02T03:04:05+00:00", dateTimeOffset];
            yield return ["ToDateTimeOffset_DateTime_Valid", "ToDateTimeOffset", typeof(DateTime?), dateTime, dateTimeOffset];
            yield return ["ToDateTimeOffset_DateTimeOffset_Valid", "ToDateTimeOffset", typeof(DateTimeOffset?), dateTimeOffset, dateTimeOffset];
            yield return ["ToDateTimeOffset_Object_Valid", "ToDateTimeOffset", typeof(object), dateTimeOffset, dateTimeOffset];

            yield return ["ToTimeSpan_String_Valid", "ToTimeSpan", typeof(string), "01:02:03", timeSpan];
            yield return ["ToTimeSpan_TimeSpan_Valid", "ToTimeSpan", typeof(TimeSpan?), timeSpan, timeSpan];
            yield return ["ToTimeSpan_Object_Valid", "ToTimeSpan", typeof(object), timeSpan, timeSpan];

            yield return ["ToGuid_String_Valid", "ToGuid", typeof(string), guid.ToString(), guid];
            yield return ["ToGuid_Guid_Valid", "ToGuid", typeof(Guid?), guid, guid];
            yield return ["ToGuid_Object_Valid", "ToGuid", typeof(object), guid, guid];

            foreach (var (methodName, sourceType) in new[]
                     {
                         ("ToDateTime", typeof(string)),
                         ("ToDateTime", typeof(DateTime?)),
                         ("ToDateTime", typeof(DateTimeOffset?)),
                         ("ToDateTime", typeof(object)),
                         ("ToDateTimeOffset", typeof(string)),
                         ("ToDateTimeOffset", typeof(DateTime?)),
                         ("ToDateTimeOffset", typeof(DateTimeOffset?)),
                         ("ToDateTimeOffset", typeof(object)),
                         ("ToTimeSpan", typeof(string)),
                         ("ToTimeSpan", typeof(TimeSpan?)),
                         ("ToTimeSpan", typeof(object)),
                         ("ToGuid", typeof(string)),
                         ("ToGuid", typeof(Guid?)),
                         ("ToGuid", typeof(object))
                     })
                yield return [$"{methodName}_{FriendlyName(sourceType)}_Null", methodName, sourceType, null, null];
        }
    }

    public static IEnumerable<object?[]> EdgeCases
    {
        get
        {
            yield return ["ToBoolean_InvalidString", "ToBoolean", typeof(string), "1", null];
            yield return ["ToByte_Max", "ToByte", typeof(string), byte.MaxValue.ToString(CultureInfo.InvariantCulture), byte.MaxValue];
            yield return ["ToByte_Overflow", "ToByte", typeof(int?), 256, null];
            yield return ["ToSByte_Min", "ToSByte", typeof(string), sbyte.MinValue.ToString(CultureInfo.InvariantCulture), sbyte.MinValue];
            yield return ["ToSByte_Max", "ToSByte", typeof(string), sbyte.MaxValue.ToString(CultureInfo.InvariantCulture), sbyte.MaxValue];
            yield return ["ToSByte_Overflow", "ToSByte", typeof(int?), 128, null];
            yield return ["ToInt16_Min", "ToInt16", typeof(string), short.MinValue.ToString(CultureInfo.InvariantCulture), short.MinValue];
            yield return ["ToInt16_Max", "ToInt16", typeof(string), short.MaxValue.ToString(CultureInfo.InvariantCulture), short.MaxValue];
            yield return ["ToInt16_Overflow", "ToInt16", typeof(int?), 32768, null];
            yield return ["ToUInt16_Max", "ToUInt16", typeof(string), ushort.MaxValue.ToString(CultureInfo.InvariantCulture), ushort.MaxValue];
            yield return ["ToUInt16_Underflow", "ToUInt16", typeof(int?), -1, null];
            yield return ["ToInt32_Min", "ToInt32", typeof(string), int.MinValue.ToString(CultureInfo.InvariantCulture), int.MinValue];
            yield return ["ToInt32_Max", "ToInt32", typeof(string), int.MaxValue.ToString(CultureInfo.InvariantCulture), int.MaxValue];
            yield return ["ToInt32_Overflow", "ToInt32", typeof(long?), (long)int.MaxValue + 1, null];
            yield return ["ToUInt32_Max", "ToUInt32", typeof(string), uint.MaxValue.ToString(CultureInfo.InvariantCulture), uint.MaxValue];
            yield return ["ToUInt32_Underflow", "ToUInt32", typeof(long?), -1L, null];
            yield return ["ToInt64_Min", "ToInt64", typeof(string), long.MinValue.ToString(CultureInfo.InvariantCulture), long.MinValue];
            yield return ["ToInt64_Max", "ToInt64", typeof(string), long.MaxValue.ToString(CultureInfo.InvariantCulture), long.MaxValue];
            yield return ["ToInt64_Overflow", "ToInt64", typeof(ulong?), (ulong)long.MaxValue + 1, null];
            yield return ["ToUInt64_Max", "ToUInt64", typeof(string), ulong.MaxValue.ToString(CultureInfo.InvariantCulture), ulong.MaxValue];
            yield return ["ToUInt64_Underflow", "ToUInt64", typeof(long?), -1L, null];
            yield return ["ToSingle_ValidBoundary", "ToSingle", typeof(string), "3.4028235E+38", float.MaxValue];
            yield return ["ToSingle_Overflow", "ToSingle", typeof(string), "1E+100", null];
            yield return ["ToDouble_ValidBoundary", "ToDouble", typeof(string), "1.7976931348623157E+308", double.MaxValue];
            yield return ["ToDouble_Overflow", "ToDouble", typeof(string), "1E+1000", null];
            yield return ["ToDecimal_ValidBoundary", "ToDecimal", typeof(string), decimal.MaxValue.ToString(CultureInfo.InvariantCulture), decimal.MaxValue];
            yield return ["ToDecimal_Overflow", "ToDecimal", typeof(string), "1E+100", null];
            yield return ["ToChar_EmptyString", "ToChar", typeof(string), string.Empty, null];
            yield return ["ToChar_IntUnderflow", "ToChar", typeof(int?), -1, null];
            yield return ["ToChar_IntOverflow", "ToChar", typeof(int?), char.MaxValue + 1, null];
            yield return ["ToDateTime_InvalidString", "ToDateTime", typeof(string), "not-a-date", null];
            yield return ["ToDateTimeOffset_InvalidString", "ToDateTimeOffset", typeof(string), "not-a-date", null];
            yield return ["ToTimeSpan_InvalidString", "ToTimeSpan", typeof(string), "not-a-duration", null];
            yield return ["ToGuid_InvalidString", "ToGuid", typeof(string), "not-a-guid", null];

            foreach (var target in NumericTargets.Keys)
            foreach (var (sourceType, value, suffix) in new[]
                     {
                         (typeof(float?), (object)float.NaN, "FloatNaN"),
                         (typeof(float?), (object)float.PositiveInfinity, "FloatPositiveInfinity"),
                         (typeof(float?), (object)float.NegativeInfinity, "FloatNegativeInfinity"),
                         (typeof(double?), (object)double.NaN, "DoubleNaN"),
                         (typeof(double?), (object)double.PositiveInfinity, "DoublePositiveInfinity"),
                         (typeof(double?), (object)double.NegativeInfinity, "DoubleNegativeInfinity")
                     })
                yield return [$"{target}_{suffix}", target, sourceType, value, null];

            foreach (var target in NumericTargets.Keys.Concat(["ToChar", "ToDateTime", "ToDateTimeOffset", "ToTimeSpan", "ToGuid"]))
                yield return [$"{target}_UnsupportedBoxedObject", target, typeof(object), new object(), null];
        }
    }

    [TestMethod]
    [DynamicData(nameof(NumericCases))]
    public void NumericOverload_ShouldHonorBehavioralContract(
        string caseName,
        string methodName,
        Type sourceType,
        object? input,
        object? expected)
    {
        _ = caseName;
        AssertConversion(methodName, sourceType, input, expected);
    }

    [TestMethod]
    [DynamicData(nameof(NonNumericCases))]
    public void NonNumericOverload_ShouldHonorBehavioralContract(
        string caseName,
        string methodName,
        Type sourceType,
        object? input,
        object? expected)
    {
        _ = caseName;
        AssertConversion(methodName, sourceType, input, expected);
    }

    [TestMethod]
    [DynamicData(nameof(EdgeCases))]
    public void ConversionBoundary_ShouldHonorBehavioralContract(
        string caseName,
        string methodName,
        Type sourceType,
        object? input,
        object? expected)
    {
        _ = caseName;
        AssertConversion(methodName, sourceType, input, expected);
    }

    [TestMethod]
    [DoNotParallelize]
    public void NumericStringConversions_ShouldUseInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pl-PL");

            Assert.AreEqual(12.5f, Library.ToSingle("12.5"));
            Assert.AreEqual(12.5d, Library.ToDouble("12.5"));
            Assert.AreEqual(12.5m, Library.ToDecimal("12.5"));
            Assert.IsNull(Library.ToDecimal("12,5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static object CreateNumericInput(string target, Type sourceType)
    {
        if (sourceType == typeof(string))
            return target == "ToBoolean" ? "true" : "42";
        if (sourceType == typeof(object))
            return 42;

        var underlyingType = Nullable.GetUnderlyingType(sourceType)!;
        if (underlyingType == typeof(bool))
            return true;
        if (underlyingType == typeof(char))
            return '*';

        return Convert.ChangeType(42, underlyingType, CultureInfo.InvariantCulture);
    }

    private static object? ExpectedNumericValue(string target, Type resultType, Type sourceType, object input)
    {
        if (target == "ToBoolean")
            return sourceType == typeof(char?) ? null : true;

        if (input is char character)
            input = (ushort)character;

        return Convert.ChangeType(input, resultType, CultureInfo.InvariantCulture);
    }

    private static object CreateCharInput(Type sourceType)
    {
        if (sourceType == typeof(string) || sourceType == typeof(object))
            return "A";

        var underlyingType = Nullable.GetUnderlyingType(sourceType)!;
        if (underlyingType == typeof(bool))
            return true;
        if (underlyingType == typeof(char))
            return 'A';

        return Convert.ChangeType(65, underlyingType, CultureInfo.InvariantCulture);
    }

    private static object? ExpectedCharValue(Type sourceType, object input)
    {
        var underlyingType = Nullable.GetUnderlyingType(sourceType);
        if (underlyingType is not null &&
            (underlyingType == typeof(float) || underlyingType == typeof(double) ||
             underlyingType == typeof(decimal) || underlyingType == typeof(bool)))
            return null;

        return sourceType == typeof(string) || sourceType == typeof(object)
            ? 'A'
            : Convert.ToChar(input, CultureInfo.InvariantCulture);
    }

    private static IEnumerable<(Type SourceType, object Input, string Expected)> ToStringInputs()
    {
        yield return (typeof(string), "text", "text");
        yield return (typeof(byte?), (byte)42, "42");
        yield return (typeof(sbyte?), (sbyte)-42, "-42");
        yield return (typeof(short?), (short)-42, "-42");
        yield return (typeof(ushort?), (ushort)42, "42");
        yield return (typeof(int?), -42, "-42");
        yield return (typeof(uint?), 42U, "42");
        yield return (typeof(long?), -42L, "-42");
        yield return (typeof(ulong?), 42UL, "42");
        yield return (typeof(float?), 12.5f, "12.5");
        yield return (typeof(double?), 12.5d, "12.5");
        yield return (typeof(decimal?), 12.5m, "12.5");
        yield return (typeof(bool?), true, "True");
        yield return (typeof(char?), 'A', "A");
        yield return (typeof(object), 42, "42");
        yield return (typeof(DateTime?), new DateTime(2024, 1, 2, 3, 4, 5), "01/02/2024 03:04:05");
        yield return (typeof(DateTimeOffset?), new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), "01/02/2024 03:04:05 +00:00");
        yield return (typeof(TimeSpan?), new TimeSpan(1, 2, 3), "01:02:03");
        yield return (typeof(Guid?), Guid.Parse("5f38db3f-6a65-4ab6-8ad4-c14d8de21a1c"), "5f38db3f-6a65-4ab6-8ad4-c14d8de21a1c");
    }

    private void AssertConversion(string methodName, Type sourceType, object? input, object? expected)
    {
        var method = typeof(LibraryBase).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [sourceType],
            modifiers: null);
        Assert.IsNotNull(method, $"Missing conversion overload {methodName}({sourceType.Name}).");

        var actual = method.Invoke(Library, [input]);
        if (expected == null)
        {
            Assert.IsNull(actual);
            return;
        }

        Assert.AreEqual(expected.GetType(), actual?.GetType());
        Assert.AreEqual(expected, actual);
    }

    private static string FriendlyName(Type type)
    {
        return Nullable.GetUnderlyingType(type)?.Name ?? type.Name;
    }
}

internal static class PrimitiveConversionDateTimeTestExtensions
{
    public static DateTime DateTimeWithoutKind(this DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }
}
