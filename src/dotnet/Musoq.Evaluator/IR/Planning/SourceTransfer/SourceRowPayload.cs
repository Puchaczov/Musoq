using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceRowPayload
{
    internal const int StructCarrierLimit = 64;

    internal static int Estimate(QueryRowShape shape, CancellationToken cancellationToken)
    {
        var size = 0;
        foreach (var field in shape.Fields)
        {
            cancellationToken.ThrowIfCancellationRequested();
            size += EstimateFieldBytes(field.FieldType);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return size;
    }

    internal static int EstimateFieldBytes(Type type)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            return IntPtr.Size;

        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean or TypeCode.Byte or TypeCode.SByte => 1,
            TypeCode.Char or TypeCode.Int16 or TypeCode.UInt16 => 2,
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
            TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double or TypeCode.DateTime => 8,
            _ when type == typeof(decimal) || type == typeof(Guid) => 16,
            _ => StructCarrierLimit + 1,
        };
    }
}
