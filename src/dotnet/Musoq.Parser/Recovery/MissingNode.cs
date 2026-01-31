using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Recovery;

/// <summary>
///     Represents a missing required node that was expected but not found.
///     The parser synthesized this placeholder to continue parsing.
/// </summary>
public class MissingNode : ErrorNode
{
    /// <summary>
    ///     Creates a new missing node.
    /// </summary>
    /// <param name="expectedDescription">Description of what was expected.</param>
    /// <param name="span">The source location where the node was expected.</param>
    public MissingNode(string expectedDescription, TextSpan span)
        : base($"Missing {expectedDescription}", span, DiagnosticCode.MQ2002_MissingToken)
    {
        ExpectedDescription = expectedDescription;
    }

    /// <summary>
    ///     Gets the description of what was expected.
    /// </summary>
    public string ExpectedDescription { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"<missing: {ExpectedDescription}>";
    }
}
