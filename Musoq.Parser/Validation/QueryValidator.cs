using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Parser.Exceptions;

namespace Musoq.Parser.Validation;

/// <summary>
/// Provides early validation of SQL queries before parsing to catch common issues
/// and provide user-friendly error messages with suggestions.
/// </summary>
public class QueryValidator
{
    private static readonly Dictionary<char, string[]> CharacterSuggestions = new()
    {
        { '`', new[] { "Use double quotes \" for identifiers", "Use single quotes ' for string literals" } },
        { '[', new[] { "Use double quotes \" for identifiers instead of square brackets" } },
        { ']', new[] { "Use double quotes \" for identifiers instead of square brackets" } },
        { '{', new[] { "Use parentheses ( ) for grouping expressions" } },
        { '}', new[] { "Use parentheses ( ) for grouping expressions" } },
        { ';', new[] { "Semicolon is not required at the end of queries in Musoq" } },
        { '\\', new[] { "Use forward slash / for division operations" } },
        { '?', new[] { "Use parameters with @ or # prefix for parameterized queries" } }
    };

    private static readonly string[] SuspiciousPatterns = new[]
    {
        @"\bDROP\s+TABLE\b",
        @"\bDELETE\s+FROM\b",
        @"\bTRUNCATE\s+TABLE\b",
        @"\bALTER\s+TABLE\b",
        @"\bCREATE\s+TABLE\b",
        @"\bINSERT\s+INTO\b",
        @"\bUPDATE\s+SET\b"
    };

    private static readonly string[] RequiredKeywords = new[] { "SELECT", "FROM" };

    /// <summary>
    /// Validates a query and returns validation issues if any are found.
    /// </summary>
    /// <param name="query">The SQL query to validate</param>
    /// <returns>List of validation issues, empty if query is valid</returns>
    public List<ValidationIssue> ValidateQuery(string query)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(query))
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.EmptyQuery,
                "Query cannot be empty or null.",
                suggestion: "Please provide a valid SQL query starting with SELECT."));
            return issues;
        }

        // Check for basic structural issues
        ValidateBasicStructure(query, issues);
        
        // Check for balanced parentheses and quotes
        ValidateBalancedDelimiters(query, issues);
        
        // Check for invalid characters
        ValidateCharacters(query, issues);
        
        // Check for suspicious patterns
        ValidateSuspiciousPatterns(query, issues);
        
        // Check for required keywords
        ValidateRequiredKeywords(query, issues);
        
        // Check query complexity
        ValidateComplexity(query, issues);

        return issues;
    }

    /// <summary>
    /// Validates a query and throws QueryValidationException if issues are found.
    /// </summary>
    /// <param name="query">The SQL query to validate</param>
    /// <exception cref="QueryValidationException">Thrown if validation issues are found</exception>
    public void ValidateAndThrow(string query)
    {
        var issues = ValidateQuery(query);
        if (issues.Any())
        {
            throw QueryValidationException.ForMultipleIssues(query, issues);
        }
    }

    private void ValidateBasicStructure(string query, List<ValidationIssue> issues)
    {
        var trimmedQuery = query.Trim();
        
        if (trimmedQuery.Length > 10000)
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.TooLong,
                "Query is too long (over 10,000 characters).",
                suggestion: "Consider breaking the query into smaller parts or simplifying the logic."));
        }
    }

    private void ValidateBalancedDelimiters(string query, List<ValidationIssue> issues)
    {
        // Check parentheses
        var parenthesesBalance = 0;
        var singleQuoteBalance = 0;
        var doubleQuoteBalance = 0;
        var inSingleQuotes = false;
        var inDoubleQuotes = false;

        for (int i = 0; i < query.Length; i++)
        {
            var ch = query[i];
            
            // Handle escaping
            if (i > 0 && query[i - 1] == '\\')
                continue;

            switch (ch)
            {
                case '\'':
                    if (!inDoubleQuotes)
                    {
                        inSingleQuotes = !inSingleQuotes;
                        singleQuoteBalance += inSingleQuotes ? 1 : -1;
                    }
                    break;
                case '"':
                    if (!inSingleQuotes)
                    {
                        inDoubleQuotes = !inDoubleQuotes;
                        doubleQuoteBalance += inDoubleQuotes ? 1 : -1;
                    }
                    break;
                case '(':
                    if (!inSingleQuotes && !inDoubleQuotes)
                        parenthesesBalance++;
                    break;
                case ')':
                    if (!inSingleQuotes && !inDoubleQuotes)
                        parenthesesBalance--;
                    break;
            }
        }

        if (parenthesesBalance != 0)
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.UnbalancedParentheses,
                parenthesesBalance > 0 
                    ? $"Missing {parenthesesBalance} closing parenthesis(es)."
                    : $"Missing {-parenthesesBalance} opening parenthesis(es).",
                suggestion: "Check that all opening parentheses have matching closing parentheses."));
        }

        if (singleQuoteBalance != 0)
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.UnbalancedQuotes,
                "Unbalanced single quotes in query.",
                suggestion: "Check that all single quotes are properly closed."));
        }

        if (doubleQuoteBalance != 0)
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.UnbalancedQuotes,
                "Unbalanced double quotes in query.",
                suggestion: "Check that all double quotes are properly closed."));
        }
    }

    private void ValidateCharacters(string query, List<ValidationIssue> issues)
    {
        var invalidChars = new List<char>();
        var positions = new List<int>();

        for (int i = 0; i < query.Length; i++)
        {
            var ch = query[i];
            if (CharacterSuggestions.ContainsKey(ch))
            {
                invalidChars.Add(ch);
                positions.Add(i);
            }
        }

        if (invalidChars.Any())
        {
            var uniqueChars = invalidChars.Distinct().ToArray();
            var suggestions = uniqueChars.SelectMany(c => CharacterSuggestions[c]).Distinct().ToArray();
            
            issues.Add(new ValidationIssue(
                ValidationIssueType.InvalidCharacters,
                $"Query contains potentially problematic characters: {string.Join(", ", uniqueChars.Select(c => $"'{c}'"))}.",
                positions.First(),
                $"Suggestions: {string.Join("; ", suggestions)}"));
        }
    }

    private void ValidateSuspiciousPatterns(string query, List<ValidationIssue> issues)
    {
        var upperQuery = query.ToUpperInvariant();
        var matchedPatterns = new List<string>();

        foreach (var pattern in SuspiciousPatterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            if (regex.IsMatch(upperQuery))
            {
                var match = regex.Match(upperQuery);
                matchedPatterns.Add(match.Value);
            }
        }

        if (matchedPatterns.Any())
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.SuspiciousPattern,
                $"Query contains patterns typically used for data modification: {string.Join(", ", matchedPatterns)}.",
                suggestion: "Musoq is designed for querying (read-only operations). Consider using appropriate data source operations if modification is intended."));
        }
    }

    private void ValidateRequiredKeywords(string query, List<ValidationIssue> issues)
    {
        var upperQuery = query.ToUpperInvariant();
        var missingKeywords = new List<string>();

        foreach (var keyword in RequiredKeywords)
        {
            if (!upperQuery.Contains(keyword))
            {
                missingKeywords.Add(keyword);
            }
        }

        if (missingKeywords.Any())
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.MissingKeywords,
                $"Query is missing required keywords: {string.Join(", ", missingKeywords)}.",
                suggestion: "Musoq queries must follow SQL syntax and include SELECT and FROM clauses."));
        }
    }

    private void ValidateComplexity(string query, List<ValidationIssue> issues)
    {
        // Count nested levels
        var maxNestingLevel = 0;
        var currentLevel = 0;
        var inQuotes = false;
        
        for (int i = 0; i < query.Length; i++)
        {
            var ch = query[i];
            
            if (ch == '\'' || ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            
            if (!inQuotes)
            {
                if (ch == '(')
                {
                    currentLevel++;
                    maxNestingLevel = Math.Max(maxNestingLevel, currentLevel);
                }
                else if (ch == ')')
                {
                    currentLevel--;
                }
            }
        }

        if (maxNestingLevel > 10)
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.TooComplex,
                $"Query has very deep nesting (level {maxNestingLevel}).",
                suggestion: "Consider breaking complex queries into simpler parts using Common Table Expressions (CTEs) or subqueries."));
        }

        // Count number of JOINs
        var joinCount = Regex.Matches(query, @"\bJOIN\b", RegexOptions.IgnoreCase).Count;
        if (joinCount > 5)
        {
            issues.Add(new ValidationIssue(
                ValidationIssueType.TooComplex,
                $"Query has many JOIN operations ({joinCount}).",
                suggestion: "Consider optimizing the query or using data source-specific operations when possible."));
        }
    }

    /// <summary>
    /// Gets suggestions for improving a query based on common patterns.
    /// </summary>
    /// <param name="query">The SQL query to analyze</param>
    /// <returns>List of suggestions for query improvement</returns>
    public List<string> GetQuerySuggestions(string query)
    {
        var suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(query))
            return suggestions;

        // Check for missing schema references
        if (query.Contains("FROM ") && !query.Contains("#"))
        {
            suggestions.Add("Consider using schema references (e.g., #schema.table) for data sources in Musoq.");
        }

        // Check for potential performance issues
        if (query.ToUpperInvariant().Contains("SELECT *"))
        {
            suggestions.Add("Consider selecting specific columns instead of * for better performance.");
        }

        // Check for missing WHERE clauses on large data sources
        var hasWhere = query.ToUpperInvariant().Contains("WHERE");
        var hasLimitOrTake = query.ToUpperInvariant().Contains("LIMIT") || query.ToUpperInvariant().Contains("TAKE");
        
        if (!hasWhere && !hasLimitOrTake && query.ToUpperInvariant().Contains("FROM"))
        {
            suggestions.Add("Consider adding WHERE clause or TAKE/LIMIT to avoid processing large datasets.");
        }

        return suggestions;
    }
}