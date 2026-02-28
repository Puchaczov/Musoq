using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Recovery;

/// <summary>
///     Panic mode recovery strategy - skips tokens until a synchronization point is found.
///     This is the most aggressive recovery strategy, used when other strategies fail.
/// </summary>
public sealed class PanicModeRecovery : IErrorRecoveryStrategy
{
    /// <summary>
    ///     Default synchronization token types for SQL.
    /// </summary>
    private static readonly HashSet<TokenType> DefaultSyncTokens = new()
    {
        TokenType.Select,
        TokenType.From,
        TokenType.Where,
        TokenType.GroupBy,
        TokenType.Having,
        TokenType.OrderBy,
        TokenType.Union,
        TokenType.Except,
        TokenType.Intersect,
        TokenType.LeftParenthesis,
        TokenType.RightParenthesis,
        TokenType.Semicolon,
        TokenType.EndOfFile
    };

    private readonly HashSet<TokenType> _syncTokens;

    /// <summary>
    ///     Creates a new panic mode recovery strategy with default sync tokens.
    /// </summary>
    public PanicModeRecovery()
        : this(DefaultSyncTokens)
    {
    }

    /// <summary>
    ///     Creates a new panic mode recovery strategy with custom sync tokens.
    /// </summary>
    public PanicModeRecovery(HashSet<TokenType> syncTokens)
    {
        _syncTokens = syncTokens;
    }

    /// <inheritdoc />
    public string Name => "PanicMode";

    /// <inheritdoc />
    public int Priority => 0; // Lowest priority - last resort

    /// <inheritdoc />
    public bool TryRecover(RecoveryContext context)
    {
        var skippedCount = 0;
        var maxSkip = 50;

        while (skippedCount < maxSkip)
        {
            var currentType = context.CurrentToken.TokenType;

            if (_syncTokens.Contains(currentType)) return skippedCount > 0;

            if (currentType == TokenType.EndOfFile) return false;


            context.SkippedTokens.Add(context.CurrentToken);
            skippedCount++;

            // Note: The actual token advancement happens in the parser
        }

        return false;
    }
}
