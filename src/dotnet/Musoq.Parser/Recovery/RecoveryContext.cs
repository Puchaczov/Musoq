using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Recovery;

/// <summary>
///     Context passed to error recovery strategies containing parser state.
/// </summary>
public sealed class RecoveryContext
{
    /// <summary>
    ///     Creates a new recovery context.
    /// </summary>
    public RecoveryContext(
        ILexer lexer,
        Token currentToken,
        Token? previousToken,
        DiagnosticBag diagnostics,
        SourceText sourceText,
        TokenType expectedTokenType,
        string? expectedSymbol = null)
    {
        Lexer = lexer;
        CurrentToken = currentToken;
        PreviousToken = previousToken;
        Diagnostics = diagnostics;
        SourceText = sourceText;
        ExpectedTokenType = expectedTokenType;
        ExpectedSymbol = expectedSymbol;
    }

    /// <summary>
    ///     The lexer being used.
    /// </summary>
    public ILexer Lexer { get; }

    /// <summary>
    ///     The current token that caused the error.
    /// </summary>
    public Token CurrentToken { get; }

    /// <summary>
    ///     The previous token before the error.
    /// </summary>
    public Token? PreviousToken { get; }

    /// <summary>
    ///     The diagnostic bag for reporting errors.
    /// </summary>
    public DiagnosticBag Diagnostics { get; }

    /// <summary>
    ///     The source text being parsed.
    /// </summary>
    public SourceText SourceText { get; }

    /// <summary>
    ///     The expected token type that was not found.
    /// </summary>
    public TokenType ExpectedTokenType { get; }

    /// <summary>
    ///     The expected symbol/keyword (optional).
    /// </summary>
    public string? ExpectedSymbol { get; }

    /// <summary>
    ///     Tokens that were skipped during recovery.
    /// </summary>
    public List<Token> SkippedTokens { get; } = new();

    /// <summary>
    ///     The recovered/synthesized token (if any).
    /// </summary>
    public Token? RecoveredToken { get; set; }

    /// <summary>
    ///     The recovered/synthesized node (if any).
    /// </summary>
    public Node? RecoveredNode { get; set; }

    /// <summary>
    ///     Gets whether recovery produced a result.
    /// </summary>
    public bool HasRecovered => RecoveredToken != null || RecoveredNode != null;
}
