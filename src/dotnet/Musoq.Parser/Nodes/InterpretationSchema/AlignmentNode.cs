using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents an alignment directive: align[bits].
///     Forces alignment to the specified bit boundary.
/// </summary>
public class AlignmentNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new alignment directive.
    /// </summary>
    /// <param name="alignmentBits">The alignment boundary in bits (typically 8, 16, or 32).</param>
    public AlignmentNode(int alignmentBits)
    {
        if (alignmentBits < 1)
            throw new ArgumentOutOfRangeException(nameof(alignmentBits), "Alignment must be at least 1 bit");

        AlignmentBits = alignmentBits;
        Id = $"{nameof(AlignmentNode)}{alignmentBits}";
    }

    /// <summary>
    ///     Gets the alignment boundary in bits.
    /// </summary>
    public int AlignmentBits { get; }

    /// <inheritdoc />
    /// <remarks>
    ///     Alignment doesn't produce a value - it's a cursor directive.
    /// </remarks>
    public override Type ClrType => typeof(void);

    /// <inheritdoc />
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
        return $"align[{AlignmentBits}]";
    }
}
