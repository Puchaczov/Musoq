#nullable enable

using System;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Recovery;

/// <summary>
///     Represents an error in the AST where a valid node could not be parsed.
///     This allows the parser to continue and report multiple errors.
/// </summary>
public class ErrorNode : Node
{
    /// <summary>
    ///     Creates a new error node.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="span">The source location of the error.</param>
    /// <param name="diagnosticCode">The diagnostic code for this error.</param>
    public ErrorNode(string message, TextSpan span,
        DiagnosticCode diagnosticCode = DiagnosticCode.MQ2001_UnexpectedToken)
    {
        Message = message;
        DiagnosticCode = diagnosticCode;
        Span = span;
        FullSpan = span;
        Id = $"{nameof(ErrorNode)}{span.Start}_{span.Length}";
    }

    /// <summary>
    ///     Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets the diagnostic code for this error.
    /// </summary>
    public DiagnosticCode DiagnosticCode { get; }

    /// <inheritdoc />
    public override Type ReturnType => typeof(void);

    /// <inheritdoc />
    public override string Id { get; }

    /// <inheritdoc />
    public override void Accept(IExpressionVisitor visitor)
    {
        if (visitor is IErrorNodeVisitor errorVisitor) errorVisitor.VisitError(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"<error: {Message}>";
    }

    /// <summary>
    ///     Creates a diagnostic from this error node.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText sourceText)
    {
        var location = sourceText.GetLocation(Span.Start);
        var contextSnippet = sourceText.GetContextSnippet(Span);

        return new Diagnostic(
            DiagnosticCode,
            DiagnosticSeverity.Error,
            Message,
            location,
            null,
            contextSnippet);
    }
}

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

/// <summary>
///     Interface for visitors that can handle error nodes.
/// </summary>
public interface IErrorNodeVisitor
{
    /// <summary>
    ///     Visits an error node.
    /// </summary>
    void VisitError(ErrorNode node);
}
