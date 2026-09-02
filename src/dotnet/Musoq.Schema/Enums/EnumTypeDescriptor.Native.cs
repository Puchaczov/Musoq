using System.Linq;
using System.Reflection;

namespace Musoq.Schema;

public sealed partial class EnumTypeDescriptor
{
    public static EnumTypeDescriptor FromClrEnum(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        if (!enumType.IsEnum)
            throw new ArgumentException($"Type '{enumType}' is not a CLR enum.", nameof(enumType));

        var underlyingType = Enum.GetUnderlyingType(enumType);
        var kind = EnumScalarTypeFacts.GetUnderlyingKind(underlyingType);
        var fields = enumType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .OrderBy(static field => field.MetadataToken)
            .Select(field => new EnumMemberDescriptor(
                field.Name,
                CreateScalar(kind, field.GetRawConstantValue())))
            .ToArray();

        return new EnumTypeDescriptor(
            enumType.FullName ?? enumType.Name,
            EnumTypeOrigin.NativeClr,
            kind,
            enumType.IsDefined(typeof(FlagsAttribute), inherit: false),
            fields);
    }

    public static bool TryNormalizeClrEnum(
        Type sourceReadType,
        out Type carrierType,
        out EnumTypeDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(sourceReadType);

        var nullableUnderlying = Nullable.GetUnderlyingType(sourceReadType);
        var enumType = nullableUnderlying ?? sourceReadType;
        if (!enumType.IsEnum)
        {
            carrierType = sourceReadType;
            descriptor = null;
            return false;
        }

        var primitiveCarrier = Enum.GetUnderlyingType(enumType);
        carrierType = nullableUnderlying == null
            ? primitiveCarrier
            : typeof(Nullable<>).MakeGenericType(primitiveCarrier);
        descriptor = FromClrEnum(enumType);
        return true;
    }

    private static EnumScalarValue CreateScalar(EnumUnderlyingKind kind, object? rawConstant)
    {
        return kind switch
        {
            EnumUnderlyingKind.Byte => EnumScalarValue.FromByte((byte)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.SByte => EnumScalarValue.FromSByte((sbyte)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.Int16 => EnumScalarValue.FromInt16((short)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.UInt16 => EnumScalarValue.FromUInt16((ushort)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.Int32 => EnumScalarValue.FromInt32((int)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.UInt32 => EnumScalarValue.FromUInt32((uint)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.Int64 => EnumScalarValue.FromInt64((long)RequireRawConstant(rawConstant)),
            EnumUnderlyingKind.UInt64 => EnumScalarValue.FromUInt64((ulong)RequireRawConstant(rawConstant)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown enum backing kind.")
        };
    }

    private static object RequireRawConstant(object? value)
    {
        return value ?? throw new InvalidOperationException("A CLR enum field has no raw constant value.");
    }
}
