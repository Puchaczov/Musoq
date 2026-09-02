using System.Globalization;

namespace Musoq.Schema;

/// <summary>
///     Allocation-free, non-boxing representation of one enum carrier value.
/// </summary>
public readonly struct EnumScalarValue : IEquatable<EnumScalarValue>
{
    private EnumScalarValue(EnumUnderlyingKind kind, ulong rawValue)
    {
        Kind = kind;
        RawValue = rawValue;
    }

    public EnumUnderlyingKind Kind { get; }

    /// <summary>
    ///     Gets the carrier bits normalized to the backing type's width.
    /// </summary>
    public ulong RawValue { get; }

    public bool IsSigned => Kind is EnumUnderlyingKind.SByte or EnumUnderlyingKind.Int16 or
        EnumUnderlyingKind.Int32 or EnumUnderlyingKind.Int64;

    public static EnumScalarValue FromRaw(EnumUnderlyingKind kind, ulong rawValue)
    {
        ValidateRawValue(kind, rawValue);
        return new EnumScalarValue(kind, rawValue);
    }

    public static EnumScalarValue FromByte(byte value) => new(EnumUnderlyingKind.Byte, value);

    public static EnumScalarValue FromSByte(sbyte value) =>
        new(EnumUnderlyingKind.SByte, unchecked((byte)value));

    public static EnumScalarValue FromInt16(short value) =>
        new(EnumUnderlyingKind.Int16, unchecked((ushort)value));

    public static EnumScalarValue FromUInt16(ushort value) => new(EnumUnderlyingKind.UInt16, value);

    public static EnumScalarValue FromInt32(int value) =>
        new(EnumUnderlyingKind.Int32, unchecked((uint)value));

    public static EnumScalarValue FromUInt32(uint value) => new(EnumUnderlyingKind.UInt32, value);

    public static EnumScalarValue FromInt64(long value) =>
        new(EnumUnderlyingKind.Int64, unchecked((ulong)value));

    public static EnumScalarValue FromUInt64(ulong value) => new(EnumUnderlyingKind.UInt64, value);

    public byte AsByte() => Kind == EnumUnderlyingKind.Byte
        ? (byte)RawValue
        : throw KindMismatch(EnumUnderlyingKind.Byte);

    public sbyte AsSByte() => Kind == EnumUnderlyingKind.SByte
        ? unchecked((sbyte)(byte)RawValue)
        : throw KindMismatch(EnumUnderlyingKind.SByte);

    public short AsInt16() => Kind == EnumUnderlyingKind.Int16
        ? unchecked((short)(ushort)RawValue)
        : throw KindMismatch(EnumUnderlyingKind.Int16);

    public ushort AsUInt16() => Kind == EnumUnderlyingKind.UInt16
        ? (ushort)RawValue
        : throw KindMismatch(EnumUnderlyingKind.UInt16);

    public int AsInt32() => Kind == EnumUnderlyingKind.Int32
        ? unchecked((int)(uint)RawValue)
        : throw KindMismatch(EnumUnderlyingKind.Int32);

    public uint AsUInt32() => Kind == EnumUnderlyingKind.UInt32
        ? (uint)RawValue
        : throw KindMismatch(EnumUnderlyingKind.UInt32);

    public long AsInt64() => Kind == EnumUnderlyingKind.Int64
        ? unchecked((long)RawValue)
        : throw KindMismatch(EnumUnderlyingKind.Int64);

    public ulong AsUInt64() => Kind == EnumUnderlyingKind.UInt64
        ? RawValue
        : throw KindMismatch(EnumUnderlyingKind.UInt64);

    public bool Equals(EnumScalarValue other)
    {
        return Kind == other.Kind && RawValue == other.RawValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is EnumScalarValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((byte)Kind, RawValue);
    }

    public override string ToString()
    {
        return Kind switch
        {
            EnumUnderlyingKind.Byte => ((byte)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.SByte => unchecked((sbyte)(byte)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.Int16 => unchecked((short)(ushort)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.UInt16 => ((ushort)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.Int32 => unchecked((int)(uint)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.UInt32 => ((uint)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.Int64 => unchecked((long)RawValue).ToString(CultureInfo.InvariantCulture),
            EnumUnderlyingKind.UInt64 => RawValue.ToString(CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Unsupported enum backing kind '{Kind}'.")
        };
    }

    public static bool operator ==(EnumScalarValue left, EnumScalarValue right) => left.Equals(right);

    public static bool operator !=(EnumScalarValue left, EnumScalarValue right) => !left.Equals(right);

    private static void ValidateRawValue(EnumUnderlyingKind kind, ulong rawValue)
    {
        var isValid = kind switch
        {
            EnumUnderlyingKind.Byte or EnumUnderlyingKind.SByte => rawValue <= byte.MaxValue,
            EnumUnderlyingKind.Int16 or EnumUnderlyingKind.UInt16 => rawValue <= ushort.MaxValue,
            EnumUnderlyingKind.Int32 or EnumUnderlyingKind.UInt32 => rawValue <= uint.MaxValue,
            EnumUnderlyingKind.Int64 or EnumUnderlyingKind.UInt64 => true,
            _ => false
        };

        if (!isValid)
            throw new ArgumentOutOfRangeException(nameof(rawValue), rawValue,
                $"Raw value does not fit enum backing kind '{kind}'.");
    }

    private InvalidOperationException KindMismatch(EnumUnderlyingKind expected)
    {
        return new InvalidOperationException(
            $"Enum scalar backing kind is '{Kind}', not the requested '{expected}'.");
    }
}
