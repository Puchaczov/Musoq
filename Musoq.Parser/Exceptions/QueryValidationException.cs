using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Exceptions;

/// <summary>
/// Exception thrown when query validation fails before parsing.
/// Provides early validation feedback to improve user experience.
/// </summary>
public class QueryValidationException : ArgumentException
{
    public string Query { get; }
    public IEnumerable<ValidationIssue> ValidationIssues { get; }

    public QueryValidationException(string query, IEnumerable<ValidationIssue> issues, string message)
        : base(message)
    {
        Query = query ?? string.Empty;
        ValidationIssues = issues ?? Enumerable.Empty<ValidationIssue>();
    }

    public static QueryValidationException ForMultipleIssues(string query, IEnumerable<ValidationIssue> issues)
    {
        var issuesList = issues.ToList();
        var issuesText = string.Join("\n", issuesList.Select(i => $"- {i.Message} (Type: {i.Type})"));
        
        var message = $"Query validation failed with {issuesList.Count} issue(s):\n{issuesText}" +
                     "\n\nPlease fix these issues before running the query.";

        return new QueryValidationException(query, issuesList, message);
    }

    public static QueryValidationException ForEmptyQuery()
    {
        var issue = new ValidationIssue(ValidationIssueType.EmptyQuery, "Query cannot be empty or null.");
        return ForMultipleIssues(string.Empty, new[] { issue });
    }

    public static QueryValidationException ForInvalidCharacters(string query, char[] invalidChars)
    {
        var charsText = string.Join(", ", invalidChars.Select(c => $"'{c}'"));
        var issue = new ValidationIssue(
            ValidationIssueType.InvalidCharacters, 
            $"Query contains invalid characters: {charsText}. Please use only valid SQL characters."
        );
        return ForMultipleIssues(query, new[] { issue });
    }

    public static QueryValidationException ForUnbalancedParentheses(string query)
    {
        var issue = new ValidationIssue(
            ValidationIssueType.UnbalancedParentheses,
            "Query has unbalanced parentheses. Please ensure all opening parentheses have matching closing parentheses."
        );
        return ForMultipleIssues(query, new[] { issue });
    }

    public static QueryValidationException ForUnbalancedQuotes(string query)
    {
        var issue = new ValidationIssue(
            ValidationIssueType.UnbalancedQuotes,
            "Query has unbalanced quotes. Please ensure all opening quotes have matching closing quotes."
        );
        return ForMultipleIssues(query, new[] { issue });
    }

    public static QueryValidationException ForSuspiciousPatterns(string query, string[] patterns)
    {
        var patternsText = string.Join(", ", patterns);
        var issue = new ValidationIssue(
            ValidationIssueType.SuspiciousPattern,
            $"Query contains potentially problematic patterns: {patternsText}. Please review your query structure."
        );
        return ForMultipleIssues(query, new[] { issue });
    }
}

/// <summary>
/// Represents a specific validation issue found in a query.
/// </summary>
public class ValidationIssue
{
    public ValidationIssueType Type { get; }
    public string Message { get; }
    public int? Position { get; }
    public string Suggestion { get; }

    public ValidationIssue(ValidationIssueType type, string message, int? position = null, string suggestion = null)
    {
        Type = type;
        Message = message ?? string.Empty;
        Position = position;
        Suggestion = suggestion ?? string.Empty;
    }
}

/// <summary>
/// Types of validation issues that can occur in queries.
/// </summary>
public enum ValidationIssueType
{
    EmptyQuery,
    InvalidCharacters,
    UnbalancedParentheses,
    UnbalancedQuotes,
    SuspiciousPattern,
    TooLong,
    TooComplex,
    MissingKeywords,
    InvalidStructure
}