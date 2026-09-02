namespace Musoq.Schema;

/// <summary>
///     Maps portable enum backing kinds to their primitive CLR carriers.
/// </summary>
public static class EnumScalarTypeFacts
{
    public static Type GetCarrierType(EnumUnderlyingKind kind)
    {
        return kind switch
        {
            EnumUnderlyingKind.Byte => typeof(byte),
            EnumUnderlyingKind.SByte => typeof(sbyte),
            EnumUnderlyingKind.Int16 => typeof(short),
            EnumUnderlyingKind.UInt16 => typeof(ushort),
            EnumUnderlyingKind.Int32 => typeof(int),
            EnumUnderlyingKind.UInt32 => typeof(uint),
            EnumUnderlyingKind.Int64 => typeof(long),
            EnumUnderlyingKind.UInt64 => typeof(ulong),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown enum backing kind.")
        };
    }

    public static EnumUnderlyingKind GetUnderlyingKind(Type carrierType)
    {
        ArgumentNullException.ThrowIfNull(carrierType);
        return Type.GetTypeCode(carrierType) switch
        {
            TypeCode.Byte => EnumUnderlyingKind.Byte,
            TypeCode.SByte => EnumUnderlyingKind.SByte,
            TypeCode.Int16 => EnumUnderlyingKind.Int16,
            TypeCode.UInt16 => EnumUnderlyingKind.UInt16,
            TypeCode.Int32 => EnumUnderlyingKind.Int32,
            TypeCode.UInt32 => EnumUnderlyingKind.UInt32,
            TypeCode.Int64 => EnumUnderlyingKind.Int64,
            TypeCode.UInt64 => EnumUnderlyingKind.UInt64,
            _ => throw new ArgumentException(
                $"Type '{carrierType}' is not a supported enum carrier type.",
                nameof(carrierType))
        };
    }
}
