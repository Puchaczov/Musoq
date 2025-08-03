using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Exceptions;

/// <summary>
/// Exception thrown when SQL syntax parsing fails.
/// Provides detailed context about the error location and helpful suggestions.
/// </summary>
public class SyntaxException : Exception
{
    public string QueryPart { get; }
    public int? Position { get; }
    public string ExpectedTokens { get; }
    public string ActualToken { get; }

    public SyntaxException(string message, string queryPart, int? position = null, string expectedTokens = null, string actualToken = null) 
        : base(message)
    {
        QueryPart = queryPart ?? string.Empty;
        Position = position;
        ExpectedTokens = expectedTokens ?? string.Empty;
        ActualToken = actualToken ?? string.Empty;
    }

    public SyntaxException(string message, string queryPart, Exception innerException, int? position = null, string expectedTokens = null, string actualToken = null) 
        : base(message, innerException)
    {
        QueryPart = queryPart ?? string.Empty;
        Position = position;
        ExpectedTokens = expectedTokens ?? string.Empty;
        ActualToken = actualToken ?? string.Empty;
    }

    public static SyntaxException ForUnexpectedToken(string actualToken, string[] expectedTokens, string queryPart, int? position = null)
    {
        var expectedList = expectedTokens?.Length > 0 ? string.Join(", ", expectedTokens) : "valid SQL token";
        var positionText = position.HasValue ? $" at position {position}" : "";
        
        var message = $"Unexpected token '{actualToken}'{positionText}. Expected: {expectedList}." +
                     $"\nQuery context: ...{queryPart}" +
                     "\n\nPlease check your SQL syntax for missing or incorrect tokens.";

        return new SyntaxException(message, queryPart, position, expectedList, actualToken);
    }

    public static SyntaxException ForMissingToken(string expectedToken, string queryPart, int? position = null)
    {
        var positionText = position.HasValue ? $" at position {position}" : "";
        
        var message = $"Missing required token '{expectedToken}'{positionText}." +
                     $"\nQuery context: ...{queryPart}" +
                     $"\n\nPlease add the missing '{expectedToken}' to your query.";

        return new SyntaxException(message, queryPart, position, expectedToken);
    }

    public static SyntaxException ForInvalidStructure(string issue, string queryPart, int? position = null)
    {
        var positionText = position.HasValue ? $" at position {position}" : "";
        
        var message = $"Invalid SQL structure{positionText}: {issue}" +
                     $"\nQuery context: ...{queryPart}" +
                     "\n\nPlease check the SQL query structure and syntax.";

        return new SyntaxException(message, queryPart, position);
    }

    public static SyntaxException ForUnsupportedSyntax(string feature, string queryPart, int? position = null)
    {
        var positionText = position.HasValue ? $" at position {position}" : "";
        
        var message = $"Unsupported SQL syntax{positionText}: {feature}" +
                     $"\nQuery context: ...{queryPart}" +
                     "\n\nPlease refer to the documentation for supported SQL syntax.";

        return new SyntaxException(message, queryPart, position);
    }

    public static SyntaxException WithSuggestions(string baseMessage, string queryPart, string[] suggestions, int? position = null)
    {
        var suggestionsText = suggestions?.Length > 0 
            ? $"\n\nSuggestions:\n{string.Join("\n", suggestions.Select(s => $"- {s}"))}"
            : "";

        var message = baseMessage + suggestionsText;

        return new SyntaxException(message, queryPart, position);
    }
}