using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a field value validation (<c>const</c>, <c>magic</c>, or <c>oneOf</c>)
///     attached to a binary schema field. Validation runs after the field is read and any
///     string modifiers are applied; a failing validation throws a parse-time validation error.
/// </summary>
public class FieldValueValidationNode : Node
{
    /// <summary>
    ///     Creates a new field value validation.
    /// </summary>
    /// <param name="kind">The validation kind.</param>
    /// <param name="values">
    ///     The expected literal value nodes. For scalar <c>const</c>/<c>magic</c> this is a single
    ///     value; for byte-list <c>const</c>/<c>magic</c> these are byte literals; for <c>oneOf</c>
    ///     these are the allowed scalar values.
    /// </param>
    /// <param name="isByteList">
    ///     True when the validation was written as a bracketed byte list (<c>const [..]</c> /
    ///     <c>magic [..]</c>); false for scalar or <c>oneOf</c> validations.
    /// </param>
    public FieldValueValidationNode(FieldValueValidationKind kind, IReadOnlyList<Node> values, bool isByteList)
    {
        ArgumentNullException.ThrowIfNull(values);

        Kind = kind;
        Values = values;
        IsByteList = isByteList;

        var valuesId = string.Concat(values.Select(value => value.Id));
        Id = $"{nameof(FieldValueValidationNode)}{kind}{isByteList}{valuesId}";
    }

    /// <summary>
    ///     Gets the validation kind.
    /// </summary>
    public FieldValueValidationKind Kind { get; }

    /// <summary>
    ///     Gets the expected literal value nodes.
    /// </summary>
    public IReadOnlyList<Node> Values { get; }

    /// <summary>
    ///     Gets a value indicating whether the validation is a bracketed byte list.
    /// </summary>
    public bool IsByteList { get; }

    /// <inheritdoc />
    public override Type ReturnType => typeof(bool);

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
        var keyword = Kind switch
        {
            FieldValueValidationKind.Const => "const",
            FieldValueValidationKind.Magic => "magic",
            FieldValueValidationKind.OneOf => "oneOf",
            _ => "const"
        };

        if (Kind != FieldValueValidationKind.OneOf && !IsByteList)
        {
            var scalar = Values.Count > 0 ? Values[0].ToString() : string.Empty;
            return $"{keyword} {scalar}";
        }

        var builder = new StringBuilder();
        builder.Append(keyword);
        builder.Append(" [");
        builder.Append(string.Join(", ", Values.Select(value => value.ToString())));
        builder.Append(']');
        return builder.ToString();
    }
}
