using System;

namespace Musoq.Parser.Nodes.From;

/// <summary>
///     Represents an Interpret/Parse call used in a FROM clause context (e.g., CROSS APPLY).
///     This node wraps the scalar result of Interpret() in an array for enumerable processing.
/// </summary>
public class InterpretFromNode : FromNode
{
    /// <summary>
    ///     Creates a new InterpretFromNode.
    /// </summary>
    /// <param name="alias">The alias for the FROM clause.</param>
    /// <param name="interpretCall">The interpret call node (InterpretCallNode, InterpretAtCallNode, or ParseCallNode).</param>
    /// <param name="applyType">The type of apply (Cross or Outer) which affects null handling.</param>
    public InterpretFromNode(string alias, Node interpretCall, ApplyType applyType)
        : base(alias)
    {
        InterpretCall = interpretCall ?? throw new ArgumentNullException(nameof(interpretCall));
        ApplyType = applyType;

        ValidateInterpretCall(interpretCall);
    }

    /// <summary>
    ///     Creates a new InterpretFromNode with return type.
    /// </summary>
    /// <param name="alias">The alias for the FROM clause.</param>
    /// <param name="interpretCall">The interpret call node.</param>
    /// <param name="applyType">The type of apply.</param>
    /// <param name="returnType">The return type of the interpreted schema.</param>
    public InterpretFromNode(string alias, Node interpretCall, ApplyType applyType, Type returnType)
        : base(alias, returnType)
    {
        InterpretCall = interpretCall ?? throw new ArgumentNullException(nameof(interpretCall));
        ApplyType = applyType;

        ValidateInterpretCall(interpretCall);
    }

    /// <summary>
    ///     Gets the interpret call node (InterpretCallNode, InterpretAtCallNode, or ParseCallNode).
    /// </summary>
    public Node InterpretCall { get; }

    /// <summary>
    ///     Gets the apply type (Cross or Outer) which determines null handling behavior.
    /// </summary>
    public ApplyType ApplyType { get; }

    /// <summary>
    ///     Gets the unique identifier for this node.
    /// </summary>
    public override string Id => $"{nameof(InterpretFromNode)}{Alias}{InterpretCall.Id}";

    /// <summary>
    ///     Gets the schema name from the wrapped interpret call.
    /// </summary>
    public string SchemaName
    {
        get
        {
            return InterpretCall switch
            {
                InterpretCallNode interpret => interpret.SchemaName,
                InterpretAtCallNode interpretAt => interpretAt.SchemaName,
                ParseCallNode parse => parse.SchemaName,
                TryInterpretCallNode tryInterpret => tryInterpret.SchemaName,
                TryParseCallNode tryParse => tryParse.SchemaName,
                _ => throw new InvalidOperationException(
                    $"Unexpected interpret call type: {InterpretCall.GetType().Name}")
            };
        }
    }

    /// <summary>
    ///     Gets whether this is a "Try" variant (TryInterpret or TryParse) that returns null on failure.
    /// </summary>
    public bool IsTryVariant => InterpretCall is TryInterpretCallNode or TryParseCallNode;

    /// <summary>
    ///     Accepts a visitor.
    /// </summary>
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    ///     Returns a string representation of this node.
    /// </summary>
    public override string ToString()
    {
        var callString = InterpretCall.ToString();
        return string.IsNullOrEmpty(Alias) ? callString : $"{callString} {Alias}";
    }

    private static void ValidateInterpretCall(Node interpretCall)
    {
        if (interpretCall is not InterpretCallNode
            && interpretCall is not InterpretAtCallNode
            && interpretCall is not ParseCallNode
            && interpretCall is not TryInterpretCallNode
            && interpretCall is not TryParseCallNode)
            throw new ArgumentException(
                $"Expected InterpretCallNode, InterpretAtCallNode, ParseCallNode, TryInterpretCallNode, or TryParseCallNode but got {interpretCall.GetType().Name}",
                nameof(interpretCall));
    }
}
