using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Recovery;

/// <summary>
///     Manages error recovery strategies and coordinates recovery attempts.
/// </summary>
public sealed class ErrorRecoveryManager
{
    private readonly List<IErrorRecoveryStrategy> _strategies;

    /// <summary>
    ///     Creates a new error recovery manager with default strategies.
    /// </summary>
    public ErrorRecoveryManager(DiagnosticBag diagnostics, SourceText sourceText)
        : this(diagnostics, sourceText, CreateDefaultStrategies())
    {
    }

    /// <summary>
    ///     Creates a new error recovery manager with custom strategies.
    /// </summary>
    public ErrorRecoveryManager(
        DiagnosticBag diagnostics,
        SourceText sourceText,
        IEnumerable<IErrorRecoveryStrategy> strategies)
    {
        Diagnostics = diagnostics;
        SourceText = sourceText;
        _strategies = strategies.OrderByDescending(s => s.Priority).ToList();
    }

    /// <summary>
    ///     Gets the diagnostic bag.
    /// </summary>
    public DiagnosticBag Diagnostics { get; }

    /// <summary>
    ///     Gets the source text.
    /// </summary>
    public SourceText SourceText { get; }

    /// <summary>
    ///     Attempts to recover from a parsing error.
    /// </summary>
    /// <param name="lexer">The lexer being used.</param>
    /// <param name="currentToken">The current token that caused the error.</param>
    /// <param name="previousToken">The previous token before the error.</param>
    /// <param name="expectedTokenType">The expected token type.</param>
    /// <param name="expectedSymbol">Optional expected symbol/keyword.</param>
    /// <returns>The recovery context with results, or null if recovery failed.</returns>
    public RecoveryContext? TryRecover(
        ILexer lexer,
        Token currentToken,
        Token? previousToken,
        TokenType expectedTokenType,
        string? expectedSymbol = null)
    {
        var context = new RecoveryContext(
            lexer,
            currentToken,
            previousToken,
            Diagnostics,
            SourceText,
            expectedTokenType,
            expectedSymbol);

        foreach (var strategy in _strategies)
            if (strategy.TryRecover(context))
                return context;

        return null;
    }

    /// <summary>
    ///     Reports a parse error and attempts recovery.
    /// </summary>
    public RecoveryContext? ReportAndRecover(
        ILexer lexer,
        Token currentToken,
        Token? previousToken,
        TokenType expectedTokenType,
        string message,
        DiagnosticCode code = DiagnosticCode.MQ2001_UnexpectedToken)
    {
        var location = SourceText.GetLocation(currentToken.Span.Start);
        var contextSnippet = SourceText.GetContextSnippet(currentToken.Span);

        var diagnostic = new Diagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            location,
            null,
            contextSnippet);

        Diagnostics.Add(diagnostic);


        return TryRecover(lexer, currentToken, previousToken, expectedTokenType);
    }

    private static IEnumerable<IErrorRecoveryStrategy> CreateDefaultStrategies()
    {
        yield return new PhraseLevelRecovery();
        yield return new PanicModeRecovery();
    }

    /// <summary>
    ///     Adds a recovery strategy.
    /// </summary>
    public void AddStrategy(IErrorRecoveryStrategy strategy)
    {
        _strategies.Add(strategy);
        _strategies.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }
}
