namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a length-bounded binary substream type annotation:
///     <c>substream[size] raw</c> or <c>substream[size] as &lt;type&gt; [exact|lax]</c>.
///     The size expression bounds a slice of the parent stream; the slice is either returned
///     raw or parsed by a nested target type that cannot read past the slice boundary.
/// </summary>
public class SubstreamTypeNode : TypeAnnotationNode
{
    /// <summary>
    ///     Creates a new substream type annotation.
    /// </summary>
    /// <param name="sizeExpression">The expression that determines the substream length in bytes.</param>
    /// <param name="mode">How the bounded slice is interpreted.</param>
    /// <param name="target">
    ///     The nested target type parsed against the bounded slice, or null when <paramref name="mode" />
    ///     is <see cref="SubstreamMode.Raw" />.
    /// </param>
    public SubstreamTypeNode(Node sizeExpression, SubstreamMode mode, TypeAnnotationNode? target)
    {
        SizeExpression = sizeExpression ?? throw new ArgumentNullException(nameof(sizeExpression));
        Mode = mode;

        if (mode == SubstreamMode.Raw)
        {
            if (target != null)
                throw new ArgumentException("Raw substreams must not declare a target type.", nameof(target));
        }
        else if (target == null)
        {
            throw new ArgumentException("Structured substreams require a target type.", nameof(target));
        }

        Target = target;
        Id = $"{nameof(SubstreamTypeNode)}{sizeExpression.Id}{mode}{target?.Id ?? string.Empty}";
    }

    /// <summary>
    ///     Gets the expression that determines the substream length in bytes.
    /// </summary>
    public Node SizeExpression { get; }

    /// <summary>
    ///     Gets how the bounded slice is interpreted.
    /// </summary>
    public SubstreamMode Mode { get; }

    /// <summary>
    ///     Gets the nested target type parsed against the bounded slice, or null for raw substreams.
    /// </summary>
    public TypeAnnotationNode? Target { get; }

    /// <inheritdoc />
    public override Type ClrType => Mode == SubstreamMode.Raw ? typeof(byte[]) : Target!.ClrType;

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
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Mode == SubstreamMode.Raw)
            return $"substream[{SizeExpression}] raw";

        var modeSuffix = Mode == SubstreamMode.Lax ? " lax" : " exact";
        return $"substream[{SizeExpression}] as {Target}{modeSuffix}";
    }
}
