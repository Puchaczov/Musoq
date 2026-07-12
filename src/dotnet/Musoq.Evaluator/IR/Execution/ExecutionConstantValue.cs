using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed class ExecutionConstantValue : IEquatable<ExecutionConstantValue>
{
    private readonly IReadOnlyList<int> _decimalBits;
    private readonly IReadOnlyList<ushort> _utf16CodeUnits;
    private readonly IReadOnlyList<byte> _guidBytes;

    private ExecutionConstantValue(
        ExecutionConstantKind kind,
        int bitWidth = 0,
        long signedInteger = 0,
        ulong unsignedInteger = 0,
        ulong floatingPointBits = 0,
        IEnumerable<int>? decimalBits = null,
        IEnumerable<ushort>? utf16CodeUnits = null,
        long ticks = 0,
        DateTimeKind dateTimeKind = DateTimeKind.Unspecified,
        int offsetMinutes = 0,
        IEnumerable<byte>? guidBytes = null,
        ExecutionTypeRef? enumType = null,
        ExecutionConstantValue? enumUnderlyingValue = null,
        ExecutionTypeRef? clrOnlyType = null,
        object? clrOnlyValue = null)
    {
        Kind = kind;
        BitWidth = bitWidth;
        SignedInteger = signedInteger;
        UnsignedInteger = unsignedInteger;
        FloatingPointBits = floatingPointBits;
        _decimalBits = Array.AsReadOnly(decimalBits?.ToArray() ?? []);
        _utf16CodeUnits = Array.AsReadOnly(utf16CodeUnits?.ToArray() ?? []);
        Ticks = ticks;
        DateTimeKind = dateTimeKind;
        OffsetMinutes = offsetMinutes;
        _guidBytes = Array.AsReadOnly(guidBytes?.ToArray() ?? []);
        EnumType = enumType;
        EnumUnderlyingValue = enumUnderlyingValue;
        ClrOnlyType = clrOnlyType;
        ClrOnlyValue = clrOnlyValue;
    }

    public ExecutionConstantKind Kind { get; }

    public int BitWidth { get; }

    public long SignedInteger { get; }

    public ulong UnsignedInteger { get; }

    public ulong FloatingPointBits { get; }

    public IReadOnlyList<int> DecimalBits => _decimalBits;

    public IReadOnlyList<ushort> Utf16CodeUnits => _utf16CodeUnits;

    public long Ticks { get; }

    public DateTimeKind DateTimeKind { get; }

    public int OffsetMinutes { get; }

    public IReadOnlyList<byte> GuidBytes => _guidBytes;

    public ExecutionTypeRef? EnumType { get; }

    public ExecutionConstantValue? EnumUnderlyingValue { get; }

    public ExecutionTypeRef? ClrOnlyType { get; }

    internal object? ClrOnlyValue { get; }

    internal static ExecutionConstantValue FromClr(object? value, ExecutionTypeRef declaredType)
    {
        ArgumentNullException.ThrowIfNull(declaredType);

        if (value is null)
            return new ExecutionConstantValue(ExecutionConstantKind.Null);

        if (value.GetType().IsEnum)
        {
            var enumType = ExecutionTypeRef.FromClr(value.GetType());
            var underlyingType = Enum.GetUnderlyingType(value.GetType());
            var underlyingValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            return new ExecutionConstantValue(
                ExecutionConstantKind.Enum,
                enumType: enumType,
                enumUnderlyingValue: FromClr(underlyingValue, ExecutionTypeRef.FromClr(underlyingType)));
        }

        return value switch
        {
            bool boolean => new ExecutionConstantValue(ExecutionConstantKind.Boolean, unsignedInteger: boolean ? 1UL : 0UL),
            char character => new ExecutionConstantValue(ExecutionConstantKind.Character, bitWidth: 16, unsignedInteger: character),
            sbyte number => Signed(number, 8),
            short number => Signed(number, 16),
            int number => Signed(number, 32),
            long number => Signed(number, 64),
            byte number => Unsigned(number, 8),
            ushort number => Unsigned(number, 16),
            uint number => Unsigned(number, 32),
            ulong number => Unsigned(number, 64),
            float number => new ExecutionConstantValue(
                ExecutionConstantKind.FloatingPoint,
                bitWidth: 32,
                floatingPointBits: unchecked((uint)BitConverter.SingleToInt32Bits(number))),
            double number => new ExecutionConstantValue(
                ExecutionConstantKind.FloatingPoint,
                bitWidth: 64,
                floatingPointBits: unchecked((ulong)BitConverter.DoubleToInt64Bits(number))),
            decimal number => new ExecutionConstantValue(ExecutionConstantKind.Decimal, decimalBits: decimal.GetBits(number)),
            string text => new ExecutionConstantValue(ExecutionConstantKind.String, utf16CodeUnits: text.Select(static character => (ushort)character)),
            DateTime dateTime => new ExecutionConstantValue(
                ExecutionConstantKind.DateTime,
                ticks: dateTime.Ticks,
                dateTimeKind: dateTime.Kind),
            DateTimeOffset dateTimeOffset => new ExecutionConstantValue(
                ExecutionConstantKind.DateTimeOffset,
                ticks: dateTimeOffset.Ticks,
                offsetMinutes: checked((int)dateTimeOffset.Offset.TotalMinutes)),
            Guid guid => new ExecutionConstantValue(ExecutionConstantKind.Guid, guidBytes: guid.ToByteArray(bigEndian: true)),
            TimeSpan timeSpan => new ExecutionConstantValue(ExecutionConstantKind.TimeSpan, ticks: timeSpan.Ticks),
            _ => new ExecutionConstantValue(
                ExecutionConstantKind.ClrOnly,
                clrOnlyType: ExecutionTypeRef.FromClr(value.GetType()),
                clrOnlyValue: value)
        };
    }

    internal object? ToClrValue()
    {
        return Kind switch
        {
            ExecutionConstantKind.Null => null,
            ExecutionConstantKind.Boolean => UnsignedInteger != 0,
            ExecutionConstantKind.Character => checked((char)UnsignedInteger),
            ExecutionConstantKind.SignedInteger => BitWidth switch
            {
                8 => (object)checked((sbyte)SignedInteger),
                16 => (object)checked((short)SignedInteger),
                32 => (object)checked((int)SignedInteger),
                64 => SignedInteger,
                _ => throw InvalidEncoding()
            },
            ExecutionConstantKind.UnsignedInteger => BitWidth switch
            {
                8 => (object)checked((byte)UnsignedInteger),
                16 => (object)checked((ushort)UnsignedInteger),
                32 => (object)checked((uint)UnsignedInteger),
                64 => UnsignedInteger,
                _ => throw InvalidEncoding()
            },
            ExecutionConstantKind.FloatingPoint => BitWidth switch
            {
                32 => (object)BitConverter.Int32BitsToSingle(unchecked((int)FloatingPointBits)),
                64 => BitConverter.Int64BitsToDouble(unchecked((long)FloatingPointBits)),
                _ => throw InvalidEncoding()
            },
            ExecutionConstantKind.Decimal when DecimalBits.Count == 4 => new decimal(DecimalBits.ToArray()),
            ExecutionConstantKind.String => new string(Utf16CodeUnits.Select(static unit => (char)unit).ToArray()),
            ExecutionConstantKind.DateTime => new DateTime(Ticks, DateTimeKind),
            ExecutionConstantKind.DateTimeOffset => new DateTimeOffset(Ticks, TimeSpan.FromMinutes(OffsetMinutes)),
            ExecutionConstantKind.Guid when GuidBytes.Count == 16 => new Guid(GuidBytes.ToArray(), bigEndian: true),
            ExecutionConstantKind.TimeSpan => new TimeSpan(Ticks),
            ExecutionConstantKind.Enum when EnumType != null && EnumUnderlyingValue != null =>
                Enum.ToObject(EnumType.ClrType, EnumUnderlyingValue.ToClrValue()!),
            ExecutionConstantKind.ClrOnly => ClrOnlyValue,
            _ => throw InvalidEncoding()
        };
    }

    internal bool TryGetInt32(out int value)
    {
        if (Kind == ExecutionConstantKind.SignedInteger && BitWidth == 32)
        {
            value = checked((int)SignedInteger);
            return true;
        }

        value = default;
        return false;
    }

    public bool Equals(ExecutionConstantValue? other)
    {
        return other is not null &&
               Kind == other.Kind &&
               BitWidth == other.BitWidth &&
               SignedInteger == other.SignedInteger &&
               UnsignedInteger == other.UnsignedInteger &&
               FloatingPointBits == other.FloatingPointBits &&
               DecimalBits.SequenceEqual(other.DecimalBits) &&
               Utf16CodeUnits.SequenceEqual(other.Utf16CodeUnits) &&
               Ticks == other.Ticks &&
               DateTimeKind == other.DateTimeKind &&
               OffsetMinutes == other.OffsetMinutes &&
               GuidBytes.SequenceEqual(other.GuidBytes) &&
               Equals(EnumType, other.EnumType) &&
               Equals(EnumUnderlyingValue, other.EnumUnderlyingValue) &&
               Equals(ClrOnlyType, other.ClrOnlyType) &&
               (Kind != ExecutionConstantKind.ClrOnly || Equals(ClrOnlyValue, other.ClrOnlyValue));
    }

    public override bool Equals(object? obj) => obj is ExecutionConstantValue other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        hash.Add(BitWidth);
        hash.Add(SignedInteger);
        hash.Add(UnsignedInteger);
        hash.Add(FloatingPointBits);
        foreach (var value in DecimalBits)
            hash.Add(value);
        foreach (var value in Utf16CodeUnits)
            hash.Add(value);
        hash.Add(Ticks);
        hash.Add(DateTimeKind);
        hash.Add(OffsetMinutes);
        foreach (var value in GuidBytes)
            hash.Add(value);
        hash.Add(EnumType);
        hash.Add(EnumUnderlyingValue);
        hash.Add(ClrOnlyType);
        if (Kind == ExecutionConstantKind.ClrOnly)
            hash.Add(ClrOnlyValue);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return Kind switch
        {
            ExecutionConstantKind.String => $"string:{string.Concat(Utf16CodeUnits.Select(static unit => unit.ToString("X4", CultureInfo.InvariantCulture)))}",
            ExecutionConstantKind.Decimal => $"decimal:{string.Join(",", DecimalBits)}",
            ExecutionConstantKind.Guid => $"guid:{Convert.ToHexString(GuidBytes.ToArray())}",
            ExecutionConstantKind.Enum => $"enum:{EnumType?.StableId}:{EnumUnderlyingValue}",
            ExecutionConstantKind.ClrOnly => $"clr-only:{ClrOnlyType?.StableId ?? "unknown"}",
            _ => $"{Kind}:{BitWidth}:{SignedInteger}:{UnsignedInteger}:{FloatingPointBits}:{Ticks}:{DateTimeKind}:{OffsetMinutes}"
        };
    }

    private static ExecutionConstantValue Signed(long value, int bitWidth) =>
        new(ExecutionConstantKind.SignedInteger, bitWidth: bitWidth, signedInteger: value);

    private static ExecutionConstantValue Unsigned(ulong value, int bitWidth) =>
        new(ExecutionConstantKind.UnsignedInteger, bitWidth: bitWidth, unsignedInteger: value);

    private InvalidOperationException InvalidEncoding() =>
        new($"Execution constant '{Kind}' has an invalid canonical encoding.");
}
