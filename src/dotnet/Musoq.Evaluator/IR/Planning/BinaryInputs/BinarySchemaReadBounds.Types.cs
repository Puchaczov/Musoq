using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class BinarySchemaReadBounds
{
    private long? TypeBits(TypeAnnotationNode type, IReadOnlyDictionary<string, long> values)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return type switch
        {
            PrimitiveTypeNode primitive => primitive.FixedSizeBytes * 8L,
            BitsTypeNode bits => bits.BitCount,
            AlignmentNode alignment => alignment.AlignmentBits - 1L,
            ByteArrayTypeNode bytes => SizedBits(Maximum(bytes.SizeExpression, values), 1),
            StringTypeNode text when text.AsTextSchemaName is null => SizedBits(Maximum(text.SizeExpression, values), 1),
            SchemaReferenceTypeNode { IsGenericInstantiation: false } reference => SizedBits(SchemaBytes(reference.SchemaName), 1),
            InlineSchemaTypeNode inline => SizedBits(FieldsBytes(inline.Fields), 1),
            BinarySwitchTypeNode choice => MaximumBranchBits(choice, values),
            ArrayTypeNode array => SizedBits(Maximum(array.SizeExpression, values), ElementBytes(array.ElementType)),
            _ => null
        };
    }

    private long? MaximumBranchBits(BinarySwitchTypeNode choice, IReadOnlyDictionary<string, long> values)
    {
        long maximum = 0;
        foreach (var branch in choice.Cases)
        {
            if (TypeBits(branch.BranchType, values) is not { } bits)
                return null;
            maximum = Math.Max(maximum, bits);
        }
        return maximum;
    }

    private long? ElementBytes(TypeAnnotationNode type)
    {
        var bits = TypeBits(type, new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase));
        return bits is >= 0 ? (bits.Value + 7) / 8 : null;
    }

    private static long? SizedBits(long? count, long? size)
    {
        if (count is null || size is null || count < 0 || size < 0 || count > int.MaxValue ||
            size > int.MaxValue || count > int.MaxValue / Math.Max(1, size.Value))
            return null;
        return count * size * 8;
    }
}
