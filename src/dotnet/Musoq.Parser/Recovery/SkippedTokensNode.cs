namespace Musoq.Parser.Recovery;

/// <summary>
///     Represents a sequence of skipped tokens during error recovery.
///     This preserves the skipped tokens in the AST for potential later analysis.
/// </summary>
public class SkippedTokensNode : ErrorNode
{
    /// <summary>
    ///     Creates a new skipped tokens node.
    /// </summary>
    /// <param name="skippedText">The text that was skipped.</param>
    /// <param name="span">The span covering all skipped tokens.</param>
    public SkippedTokensNode(string skippedText, TextSpan span)
        : base($"Skipped: {skippedText}", span)
    {
        SkippedText = skippedText;
    }

    /// <summary>
    ///     Gets the text that was skipped.
    /// </summary>
    public string SkippedText { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"<skipped: {SkippedText}>";
    }
}
