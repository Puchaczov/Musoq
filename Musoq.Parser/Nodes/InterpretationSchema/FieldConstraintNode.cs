using System;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Represents a check constraint on a field value.
///     Constraints are validated at parse time; failure produces an error.
/// </summary>
public class FieldConstraintNode : Node
{
    /// <summary>
    ///     Creates a new field constraint.
    /// </summary>
    /// <param name="expression">The boolean expression to evaluate.</param>
    public FieldConstraintNode(Node expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Id = $"{nameof(FieldConstraintNode)}{expression.Id}";
    }

    /// <summary>
    ///     Gets the constraint expression.
    ///     Must evaluate to a boolean at parse time.
    /// </summary>
    public Node Expression { get; }

    /// <inheritdoc />
    public override Type ReturnType => typeof(bool);

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
        return $"check({Expression.ToString()})";
    }
}
