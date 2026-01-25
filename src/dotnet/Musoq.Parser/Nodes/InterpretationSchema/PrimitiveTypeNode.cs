using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a primitive type annotation (byte, short, int, long, float, double, etc.)
///     with optional endianness specification.
/// </summary>
public class PrimitiveTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new primitive type annotation.
    /// </summary>
    /// <param name="typeName">The primitive type name.</param>
    /// <param name="endianness">The byte order for multi-byte types.</param>
    public PrimitiveTypeNode(PrimitiveTypeName typeName, Endianness endianness)
    {
        TypeName = typeName;
        Endianness = endianness;
        Id = $"{nameof(PrimitiveTypeNode)}{typeName}{endianness}";
    }

    /// <summary>
    ///     Gets the primitive type name.
    /// </summary>
    public PrimitiveTypeName TypeName { get; }

    /// <summary>
    ///     Gets the endianness for this type.
    /// </summary>
    public Endianness Endianness { get; }

    /// <inheritdoc />
    public override Type ClrType => TypeName switch
    {
        PrimitiveTypeName.Byte => typeof(byte),
        PrimitiveTypeName.SByte => typeof(sbyte),
        PrimitiveTypeName.Short => typeof(short),
        PrimitiveTypeName.UShort => typeof(ushort),
        PrimitiveTypeName.Int => typeof(int),
        PrimitiveTypeName.UInt => typeof(uint),
        PrimitiveTypeName.Long => typeof(long),
        PrimitiveTypeName.ULong => typeof(ulong),
        PrimitiveTypeName.Float => typeof(float),
        PrimitiveTypeName.Double => typeof(double),
        _ => throw new InvalidOperationException($"Unknown primitive type: {TypeName}")
    };

    /// <inheritdoc />
    public override bool IsFixedSize => true;

    /// <inheritdoc />
    public override int? FixedSizeBytes => TypeName switch
    {
        PrimitiveTypeName.Byte => 1,
        PrimitiveTypeName.SByte => 1,
        PrimitiveTypeName.Short => 2,
        PrimitiveTypeName.UShort => 2,
        PrimitiveTypeName.Int => 4,
        PrimitiveTypeName.UInt => 4,
        PrimitiveTypeName.Long => 8,
        PrimitiveTypeName.ULong => 8,
        PrimitiveTypeName.Float => 4,
        PrimitiveTypeName.Double => 8,
        _ => throw new InvalidOperationException($"Unknown primitive type: {TypeName}")
    };

    /// <inheritdoc />
    public override Type ReturnType => ClrType;

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var typeName = TypeName.ToString().ToLowerInvariant();
        return Endianness switch
        {
            Endianness.LittleEndian => $"{typeName} le",
            Endianness.BigEndian => $"{typeName} be",
            _ => typeName
        };
    }
}
