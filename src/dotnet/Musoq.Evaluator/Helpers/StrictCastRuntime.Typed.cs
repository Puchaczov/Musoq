using System.Globalization;

namespace Musoq.Evaluator.Helpers;

/// <summary>Concrete conversion calls retain IConvertible through constrained value-type dispatch.</summary>
public static partial class StrictCastRuntime
{
    public static bool? ToBoolean<T>(T value) where T : struct, IConvertible
        => value.ToBoolean(CultureInfo.InvariantCulture);

    public static bool? ToBoolean<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToBoolean(CultureInfo.InvariantCulture) : null;

    public static bool? ToBoolean(string? value)
        => value is null ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    public static byte? ToByte<T>(T value) where T : struct, IConvertible
        => value.ToByte(CultureInfo.InvariantCulture);

    public static byte? ToByte<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToByte(CultureInfo.InvariantCulture) : null;

    public static byte? ToByte(string? value)
        => value is null ? null : Convert.ToByte(value, CultureInfo.InvariantCulture);

    public static sbyte? ToSByte<T>(T value) where T : struct, IConvertible
        => value.ToSByte(CultureInfo.InvariantCulture);

    public static sbyte? ToSByte<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToSByte(CultureInfo.InvariantCulture) : null;

    public static sbyte? ToSByte(string? value)
        => value is null ? null : Convert.ToSByte(value, CultureInfo.InvariantCulture);

    public static short? ToInt16<T>(T value) where T : struct, IConvertible
        => value.ToInt16(CultureInfo.InvariantCulture);

    public static short? ToInt16<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToInt16(CultureInfo.InvariantCulture) : null;

    public static short? ToInt16(string? value)
        => value is null ? null : Convert.ToInt16(value, CultureInfo.InvariantCulture);

    public static ushort? ToUInt16<T>(T value) where T : struct, IConvertible
        => value.ToUInt16(CultureInfo.InvariantCulture);

    public static ushort? ToUInt16<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToUInt16(CultureInfo.InvariantCulture) : null;

    public static ushort? ToUInt16(string? value)
        => value is null ? null : Convert.ToUInt16(value, CultureInfo.InvariantCulture);

    public static int? ToInt32<T>(T value) where T : struct, IConvertible
        => value.ToInt32(CultureInfo.InvariantCulture);

    public static int? ToInt32<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToInt32(CultureInfo.InvariantCulture) : null;

    public static int? ToInt32(string? value)
        => value is null ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    public static uint? ToUInt32<T>(T value) where T : struct, IConvertible
        => value.ToUInt32(CultureInfo.InvariantCulture);

    public static uint? ToUInt32<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToUInt32(CultureInfo.InvariantCulture) : null;

    public static uint? ToUInt32(string? value)
        => value is null ? null : Convert.ToUInt32(value, CultureInfo.InvariantCulture);

    public static long? ToInt64<T>(T value) where T : struct, IConvertible
        => value.ToInt64(CultureInfo.InvariantCulture);

    public static long? ToInt64<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToInt64(CultureInfo.InvariantCulture) : null;

    public static long? ToInt64(string? value)
        => value is null ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);

    public static ulong? ToUInt64<T>(T value) where T : struct, IConvertible
        => value.ToUInt64(CultureInfo.InvariantCulture);

    public static ulong? ToUInt64<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToUInt64(CultureInfo.InvariantCulture) : null;

    public static ulong? ToUInt64(string? value)
        => value is null ? null : Convert.ToUInt64(value, CultureInfo.InvariantCulture);

    public static float? ToSingle<T>(T value) where T : struct, IConvertible
        => value.ToSingle(CultureInfo.InvariantCulture);

    public static float? ToSingle<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToSingle(CultureInfo.InvariantCulture) : null;

    public static float? ToSingle(string? value)
        => value is null ? null : Convert.ToSingle(value, CultureInfo.InvariantCulture);

    public static double? ToDouble<T>(T value) where T : struct, IConvertible
        => value.ToDouble(CultureInfo.InvariantCulture);

    public static double? ToDouble<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToDouble(CultureInfo.InvariantCulture) : null;

    public static double? ToDouble(string? value)
        => value is null ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);

    public static decimal? ToDecimal<T>(T value) where T : struct, IConvertible
        => value.ToDecimal(CultureInfo.InvariantCulture);

    public static decimal? ToDecimal<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToDecimal(CultureInfo.InvariantCulture) : null;

    public static decimal? ToDecimal(string? value)
        => value is null ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    public static char? ToChar<T>(T value) where T : struct, IConvertible
        => value.ToChar(CultureInfo.InvariantCulture);

    public static char? ToChar<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToChar(CultureInfo.InvariantCulture) : null;

    public static char? ToChar(string? value)
        => value is null ? null : char.Parse(value);

    public static DateTime? ToDateTime<T>(T value) where T : struct, IConvertible
        => value.ToDateTime(CultureInfo.InvariantCulture);

    public static DateTime? ToDateTime<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToDateTime(CultureInfo.InvariantCulture) : null;

    public static DateTime? ToDateTime(string? value)
        => value is null ? null : Convert.ToDateTime(value, CultureInfo.InvariantCulture);

    public static string ToString<T>(T value) where T : struct, IConvertible
        => value.ToString(CultureInfo.InvariantCulture);

    public static string? ToString<T>(T? value) where T : struct, IConvertible
        => value.HasValue ? value.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) : null;

    public static string? ToString(string? value) => value;

    public static Guid? ToGuid(Guid value) => value;
    public static Guid? ToGuid(Guid? value) => value;
    public static Guid? ToGuid(string? value)
        => value is null ? null : Guid.Parse(value);

    public static Guid? ToGuid<T>(T value) where T : struct
        => throw new InvalidCastException($"Cannot cast value of type '{typeof(T).Name}' to Guid.");
    public static Guid? ToGuid<T>(T? value) where T : struct
        => value.HasValue ? ToGuid(value.GetValueOrDefault()) : null;

    public static string ToString(Guid value) => value.ToString();
    public static string? ToString(Guid? value) => value.HasValue ? ToString(value.GetValueOrDefault()) : null;

    public static TimeSpan? ToTimeSpan(TimeSpan value) => value;
    public static TimeSpan? ToTimeSpan(TimeSpan? value) => value;
    public static TimeSpan? ToTimeSpan(string? value)
        => value is null ? null : TimeSpan.Parse(value, CultureInfo.InvariantCulture);

    public static TimeSpan? ToTimeSpan<T>(T value) where T : struct
        => throw new InvalidCastException($"Cannot cast value of type '{typeof(T).Name}' to TimeSpan.");
    public static TimeSpan? ToTimeSpan<T>(T? value) where T : struct
        => value.HasValue ? ToTimeSpan(value.GetValueOrDefault()) : null;

    public static string ToString(TimeSpan value) => value.ToString(null, CultureInfo.InvariantCulture);
    public static string? ToString(TimeSpan? value) => value.HasValue ? ToString(value.GetValueOrDefault()) : null;

    public static DateTimeOffset? ToDateTimeOffset(DateTimeOffset value) => value;
    public static DateTimeOffset? ToDateTimeOffset(DateTimeOffset? value) => value;
    public static DateTimeOffset? ToDateTimeOffset(string? value)
        => value is null ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

    public static DateTimeOffset? ToDateTimeOffset<T>(T value) where T : struct
        => throw new InvalidCastException($"Cannot cast value of type '{typeof(T).Name}' to DateTimeOffset.");
    public static DateTimeOffset? ToDateTimeOffset<T>(T? value) where T : struct
        => value.HasValue ? ToDateTimeOffset(value.GetValueOrDefault()) : null;

    public static string ToString(DateTimeOffset value) => value.ToString(null, CultureInfo.InvariantCulture);
    public static string? ToString(DateTimeOffset? value) => value.HasValue ? ToString(value.GetValueOrDefault()) : null;

    public static DateTimeOffset? ToDateTimeOffset(DateTime value) => new DateTimeOffset(value);
    public static DateTimeOffset? ToDateTimeOffset(DateTime? value)
        => value.HasValue ? new DateTimeOffset(value.GetValueOrDefault()) : null;
    public static string? ToValueString<T>(T value) where T : struct => value.ToString();
    public static string? ToValueString<T>(T? value) where T : struct
        => value.HasValue ? value.GetValueOrDefault().ToString() : null;
    public static string? ToReferenceString<T>(T? value) where T : class => value?.ToString();
}