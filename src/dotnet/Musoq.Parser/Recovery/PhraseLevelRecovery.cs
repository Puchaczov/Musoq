#nullable enable

using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Recovery;

/// <summary>
///     Phrase-level recovery strategy - tries to complete the current phrase by
///     inserting missing tokens or making small adjustments.
/// </summary>
public sealed class PhraseLevelRecovery : IErrorRecoveryStrategy
{
    /// <summary>
    ///     Token pairs that can be auto-completed.
    /// </summary>
    private static readonly Dictionary<TokenType, TokenType> MatchingPairs = new()
    {
        { TokenType.LeftParenthesis, TokenType.RightParenthesis },
        { TokenType.Function, TokenType.RightParenthesis }
    };

    /// <summary>
    ///     Tokens that can be safely inserted to complete expressions.
    /// </summary>
    private static readonly HashSet<TokenType> InsertableTokens = new()
    {
        TokenType.RightParenthesis,
        TokenType.Comma,
        TokenType.Word // For missing identifiers
    };

    /// <inheritdoc />
    public string Name => "PhraseLevel";

    /// <inheritdoc />
    public int Priority => 100; // High priority - try this first

    /// <inheritdoc />
    public bool TryRecover(RecoveryContext context)
    {
        if (TryInsertClosingDelimiter(context))
            return true;


        if (TryInsertMissingToken(context))
            return true;


        if (TryDeleteExtraneousToken(context))
            return true;

        return false;
    }

    private bool TryInsertClosingDelimiter(RecoveryContext context)
    {
        if (context.ExpectedTokenType == TokenType.RightParenthesis)
        {
            var syntheticToken = CreateSyntheticToken(
                context.ExpectedTokenType,
                ")",
                context.PreviousToken?.Span.End ?? context.CurrentToken.Span.Start);

            context.RecoveredToken = syntheticToken;


            AddMissingTokenDiagnostic(context, ")");

            return true;
        }

        return false;
    }

    private bool TryInsertMissingToken(RecoveryContext context)
    {
        if (InsertableTokens.Contains(context.ExpectedTokenType))
        {
            var value = GetDefaultValueForToken(context.ExpectedTokenType, context.ExpectedSymbol);
            var position = context.PreviousToken?.Span.End ?? context.CurrentToken.Span.Start;

            var syntheticToken = CreateSyntheticToken(
                context.ExpectedTokenType,
                value,
                position);

            context.RecoveredToken = syntheticToken;


            AddMissingTokenDiagnostic(context, value);

            return true;
        }

        return false;
    }

    private bool TryDeleteExtraneousToken(RecoveryContext context)
    {
        var currentType = context.CurrentToken.TokenType;
        var previousType = context.PreviousToken?.TokenType;


        if (currentType == TokenType.Comma && previousType == TokenType.Comma)
        {
            context.SkippedTokens.Add(context.CurrentToken);

            AddExtraneousTokenDiagnostic(context);

            return true;
        }

        return false;
    }

    private static Token CreateSyntheticToken(TokenType type, string value, int position)
    {
        var span = new TextSpan(position, 0);
        return new SyntheticToken(value, type, span);
    }

    private static string GetDefaultValueForToken(TokenType type, string? expectedSymbol)
    {
        if (!string.IsNullOrEmpty(expectedSymbol))
            return expectedSymbol;

        return type switch
        {
            TokenType.RightParenthesis => ")",
            TokenType.LeftParenthesis => "(",
            TokenType.Comma => ",",
            TokenType.Word => "<missing>",
            _ => "<missing>"
        };
    }

    private static void AddMissingTokenDiagnostic(RecoveryContext context, string tokenValue)
    {
        var location = context.SourceText.GetLocation(
            context.PreviousToken?.Span.End ?? context.CurrentToken.Span.Start);

        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2002_MissingToken,
            DiagnosticSeverity.Error,
            $"Missing '{tokenValue}'",
            location);

        context.Diagnostics.Add(diagnostic);
    }

    private static void AddExtraneousTokenDiagnostic(RecoveryContext context)
    {
        var location = context.SourceText.GetLocation(context.CurrentToken.Span.Start);

        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            $"Unexpected token '{context.CurrentToken.Value}'",
            location,
            null,
            context.SourceText.GetContextSnippet(context.CurrentToken.Span));

        context.Diagnostics.Add(diagnostic);
    }
}

/// <summary>
///     A synthetic token created during error recovery.
/// </summary>
internal sealed class SyntheticToken : Token
{
    public SyntheticToken(string value, TokenType type, TextSpan span)
        : base(value, type, span)
    {
        IsSynthetic = true;
    }

    /// <summary>
    ///     Gets a value indicating whether this token was synthesized during error recovery.
    /// </summary>
    public bool IsSynthetic { get; }

    /// <inheritdoc />
    public override GenericToken<TokenType> Clone()
    {
        return new SyntheticToken(Value, TokenType, Span);
    }
}
