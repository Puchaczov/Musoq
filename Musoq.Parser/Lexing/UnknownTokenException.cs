using System;
using System.Linq;

namespace Musoq.Parser.Lexing;

/// <summary>
/// Exception thrown when the lexer encounters an unrecognized character or token sequence.
/// Provides context about the location and suggestions for resolution.
/// </summary>
public class UnknownTokenException : Exception
{
    public int Position { get; }
    public char UnknownCharacter { get; }
    public string RemainingQuery { get; }
    public string SurroundingContext { get; }

    public UnknownTokenException(int position, char unknownCharacter, string remainingQuery, string surroundingContext = null)
        : base(GenerateMessage(position, unknownCharacter, remainingQuery, surroundingContext))
    {
        Position = position;
        UnknownCharacter = unknownCharacter;
        RemainingQuery = remainingQuery ?? string.Empty;
        SurroundingContext = surroundingContext ?? string.Empty;
    }

    private static string GenerateMessage(int position, char unknownCharacter, string remainingQuery, string surroundingContext)
    {
        var contextInfo = !string.IsNullOrEmpty(surroundingContext) 
            ? $"\nNear: '{surroundingContext}'" 
            : string.Empty;

        var suggestions = GetSuggestions(unknownCharacter);
        var suggestionsText = suggestions.Any() 
            ? $"\n\nDid you mean: {string.Join(", ", suggestions)}" 
            : string.Empty;

        return $"Unrecognized character '{unknownCharacter}' at position {position}.{contextInfo}" +
               $"\nRemaining query: '{remainingQuery?.Substring(0, Math.Min(50, remainingQuery?.Length ?? 0))}'" +
               (remainingQuery?.Length > 50 ? "..." : "") +
               suggestionsText +
               "\n\nPlease check your SQL syntax for typos or unsupported characters.";
    }

    private static string[] GetSuggestions(char unknownChar)
    {
        return unknownChar switch
        {
            '`' => new[] { "Use double quotes \" for identifiers", "Use single quotes ' for strings" },
            '[' or ']' => new[] { "Use double quotes \" for identifiers instead of brackets" },
            '{' or '}' => new[] { "Use parentheses ( ) for grouping expressions" },
            ';' => new[] { "Semicolon is not required at the end of queries" },
            '\\' => new[] { "Use forward slash / for division" },
            '?' => new[] { "Use parameters with @ or # prefix" },
            _ => new string[0]
        };
    }

    public static UnknownTokenException ForInvalidCharacter(int position, char character, string fullQuery)
    {
        var start = Math.Max(0, position - 10);
        var end = Math.Min(fullQuery.Length, position + 10);
        var surroundingContext = fullQuery.Substring(start, end - start);
        var remainingQuery = fullQuery.Substring(position);

        return new UnknownTokenException(position, character, remainingQuery, surroundingContext);
    }
}