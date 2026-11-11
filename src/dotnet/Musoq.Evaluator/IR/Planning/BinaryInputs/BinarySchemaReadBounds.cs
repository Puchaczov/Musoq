using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Planning;

/// <summary>Conservative byte extents; unknown expressions retain complete input snapshots.</summary>
internal sealed partial class BinarySchemaReadBounds(SchemaRegistry registry, CancellationToken cancellationToken)
{
    private readonly HashSet<string> _activeSchemas = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long?> _bounds = new(StringComparer.OrdinalIgnoreCase);

    public int? MaximumBytes(string name)
    {
        var size = SchemaBytes(name);
        return size is >= 0 and <= int.MaxValue ? (int)size.Value : null;
    }

    private long? SchemaBytes(string name)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_bounds.TryGetValue(name, out var cached))
            return cached;
        if (!_activeSchemas.Add(name))
            return null;
        try
        {
            if (!registry.TryGetSchema(name, out var registration) || registration?.Node is not BinarySchemaNode { IsGeneric: false } schema)
                return null;
            var result = FieldsBytes(schema.Fields, schema.Extends);
            _bounds[name] = result;
            return result;
        }
        finally
        {
            _activeSchemas.Remove(name);
        }
    }

    private long? FieldsBytes(IReadOnlyList<SchemaFieldNode> fields, string? parent = null)
    {
        var values = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long bits = 0;
        if (parent is not null)
        {
            var inherited = SchemaBytes(parent);
            if (inherited is null)
                return null;
            bits = inherited.Value * 8;
        }

        foreach (var source in fields)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source is not FieldDefinitionNode field || field.Constraint is not null ||
                field.WhenCondition is not null && !IsValueExpression(field.WhenCondition))
                return null;
            if (field.AtOffset is not null)
            {
                var offset = Maximum(field.AtOffset, values);
                if (offset is null or > int.MaxValue)
                    return null;
                bits = Math.Max(bits, offset.Value * 8);
            }
            var extent = TypeBits(field.TypeAnnotation, values);
            if (extent is null)
                return null;
            if (field.TypeAnnotation is not BitsTypeNode)
                bits = (bits + 7) / 8 * 8;
            bits += extent.Value;
            if (bits > (long)int.MaxValue * 8)
                return null;
            if (field.TypeAnnotation is PrimitiveTypeNode primitive)
                values[field.Name] = PrimitiveMaximum(primitive.TypeName);
            else if (field.TypeAnnotation is BitsTypeNode { BitCount: < 31 } bitField)
                values[field.Name] = (1L << bitField.BitCount) - 1;
        }
        return (bits + 7) / 8;
    }

    private static long PrimitiveMaximum(PrimitiveTypeName type) => type switch
    {
        PrimitiveTypeName.Byte => byte.MaxValue,
        PrimitiveTypeName.SByte => sbyte.MaxValue,
        PrimitiveTypeName.Short => short.MaxValue,
        PrimitiveTypeName.UShort => ushort.MaxValue,
        PrimitiveTypeName.Int => int.MaxValue,
        PrimitiveTypeName.UInt => uint.MaxValue,
        _ => long.MaxValue
    };

    private static long? Maximum(Node expression, IReadOnlyDictionary<string, long> values) => expression switch
    {
        IntegerNode integer when long.TryParse(Convert.ToString(integer.ObjValue, CultureInfo.InvariantCulture),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0 => value,
        IdentifierNode identifier when values.TryGetValue(identifier.Name, out var maximum) => maximum,
        AccessColumnNode column when values.TryGetValue(column.Name, out var maximum) => maximum,
        _ => null
    };

    private static bool IsValueExpression(Node expression) => expression switch
    {
        IntegerNode or BooleanNode or IdentifierNode or AccessColumnNode => true,
        BinaryNode binary => IsValueExpression(binary.Left) && IsValueExpression(binary.Right),
        NotNode not => IsValueExpression(not.Expression),
        _ => false
    };
}
