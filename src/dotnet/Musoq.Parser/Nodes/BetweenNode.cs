using System;

namespace Musoq.Parser.Nodes;

/// <summary>
///     Represents a SQL BETWEEN expression: expression BETWEEN min AND max
///     This is equivalent to: expression >= min AND expression <= max
/// </summary>
public class BetweenNode : Node
{
    public BetweenNode(Node expression, Node min, Node max)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Min = min ?? throw new ArgumentNullException(nameof(min));
        Max = max ?? throw new ArgumentNullException(nameof(max));

        // Compute span from children if available
        if (expression.HasSpan && max.HasSpan)
        {
            Span = ComputeSpan(expression, max);
            FullSpan = Span;
        }

        Id = CalculateId();
    }

    /// <summary>
    ///     The expression being tested (left side of BETWEEN).
    /// </summary>
    public Node Expression { get; }

    /// <summary>
    ///     The minimum value (lower bound).
    /// </summary>
    public Node Min { get; }

    /// <summary>
    ///     The maximum value (upper bound).
    /// </summary>
    public Node Max { get; }

    public override Type ReturnType => typeof(bool);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Expression.ToString()} between {Min.ToString()} and {Max.ToString()}";
    }

    private string CalculateId()
    {
        return $"{nameof(BetweenNode)}{Expression.Id}{Min.Id}{Max.Id}";
    }
}
