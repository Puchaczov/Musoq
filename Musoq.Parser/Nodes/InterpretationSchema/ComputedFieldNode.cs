#nullable enable
using System;
using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a computed field definition within a binary or text schema.
///     Computed fields derive their value from an expression and do not consume any bytes.
/// </summary>
/// <example>
///     binary Packet {
///     RawFlags: short le,
///     IsCompressed: (RawFlags &amp; 0x01) &lt;&gt; 0,
///     IsEncrypted:  (RawFlags &amp; 0x02) &lt;&gt; 0,
///     Priority:     (RawFlags >> 4) &amp; 0x0F
///     }
/// </example>
public class ComputedFieldNode : SchemaFieldNode
{
    /// <summary>
    ///     Creates a new computed field definition.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="expression">The expression that computes the field value.</param>
    public ComputedFieldNode(string name, Node expression)
        : base(name)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Id = $"{nameof(ComputedFieldNode)}{name}{expression.Id}";
    }

    /// <summary>
    ///     Gets the expression that computes the field value.
    /// </summary>
    public Node Expression { get; }

    /// <inheritdoc />
    public override bool IsComputed => true;

    /// <inheritdoc />
    public override Type ReturnType => Expression.ReturnType;

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
        var builder = new StringBuilder();
        builder.Append(Name);
        builder.Append(": ");
        builder.Append(Expression.ToString());
        return builder.ToString();
    }
}
