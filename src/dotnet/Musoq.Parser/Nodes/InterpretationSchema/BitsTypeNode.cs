using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a bit field type annotation: bits[count].
///     Used for sub-byte data extraction in binary schemas.
/// </summary>
public class BitsTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new bit field type annotation.
    /// </summary>
    /// <param name="bitCount">The number of bits (1-64).</param>
    public BitsTypeNode(int bitCount)
    {
        if (bitCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "Bit count must be between 1 and 64");

        BitCount = bitCount;
        Id = $"{nameof(BitsTypeNode)}{bitCount}";
    }

    /// <summary>
    ///     Gets the number of bits in this field.
    /// </summary>
    public int BitCount { get; }

    /// <inheritdoc />
    /// <remarks>
    ///     Type depends on bit count: 1-8 → byte, 9-16 → ushort, 17-32 → uint, 33-64 → ulong.
    /// </remarks>
    public override Type ClrType => BitCount switch
    {
        <= 8 => typeof(byte),
        <= 16 => typeof(ushort),
        <= 32 => typeof(uint),
        _ => typeof(ulong)
    };

    /// <inheritdoc />
    /// <remarks>
    ///     Bit fields are not fixed-size in terms of bytes because they may not
    ///     align to byte boundaries.
    /// </remarks>
    public override bool IsFixedSize => false;

    /// <inheritdoc />
    public override int? FixedSizeBytes => null;

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
        return $"bits[{BitCount}]";
    }
}
