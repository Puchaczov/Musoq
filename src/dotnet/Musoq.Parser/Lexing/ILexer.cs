using System;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Lexer interface for SQL tokenization.
/// </summary>
public interface ILexer
{
    /// <summary>
    ///     Gets the current position.
    /// </summary>
    int Position { get; }

    /// <summary>
    ///     Gets the already resolved (parsed) portion of the query for error reporting.
    /// </summary>
    string AlreadyResolvedQueryPart { get; }

    /// <summary>
    ///     Gets or sets whether the lexer is in schema parsing context.
    /// </summary>
    bool IsSchemaContext { get; set; }

    /// <summary>
    ///     Gets the original input string.
    /// </summary>
    string Input { get; }

    /// <summary>
    ///     Gets the source text for the input.
    /// </summary>
    SourceText SourceText { get; }

    /// <summary>
    ///     Gets the diagnostic bag for collecting errors.
    /// </summary>
    DiagnosticBag Diagnostics { get; }

    /// <summary>
    ///     Gets or sets whether to recover from errors instead of throwing.
    /// </summary>
    bool RecoverOnError { get; set; }

    /// <summary>
    ///     Gets lastly taken token from stream.
    /// </summary>
    /// <returns>The Token.</returns>
    Token Last();

    /// <summary>
    ///     Gets the currently computed token.
    /// </summary>
    /// <returns>The Token.</returns>
    Token Current();

    /// <summary>
    ///     Compute the next token from stream.
    /// </summary>
    /// <returns>The Token.</returns>
    Token Next();

    /// <summary>
    ///     Gets the next token that matches the specified regex.
    /// </summary>
    /// <param name="regex">The regex to match.</param>
    /// <param name="getToken">Function to create a token from the matched value.</param>
    /// <returns>The matched token.</returns>
    Token NextOf(Regex regex, Func<string, Token> getToken);
}
