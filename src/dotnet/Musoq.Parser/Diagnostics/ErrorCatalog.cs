#nullable enable

using System;
using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Catalog of error messages for all diagnostic codes.
///     Provides localized and formatted error messages.
/// </summary>
public static class ErrorCatalog
{
    private static readonly Dictionary<DiagnosticCode, string> MessageTemplates = new()
    {
        // Lexer Errors (MQ1xxx)
        [DiagnosticCode.MQ1001_UnknownToken] = "Unknown token '{0}'",
        [DiagnosticCode.MQ1002_UnterminatedString] = "Unterminated string literal",
        [DiagnosticCode.MQ1003_InvalidNumericLiteral] = "Invalid numeric literal '{0}'",
        [DiagnosticCode.MQ1004_InvalidEscapeSequence] = "Invalid escape sequence '{0}'",
        [DiagnosticCode.MQ1005_UnterminatedBlockComment] = "Unterminated block comment",
        [DiagnosticCode.MQ1006_InvalidHexNumber] = "Invalid hexadecimal number '{0}'",
        [DiagnosticCode.MQ1007_InvalidBinaryNumber] = "Invalid binary number '{0}'",
        [DiagnosticCode.MQ1008_InvalidOctalNumber] = "Invalid octal number '{0}'",

        // Parser/Syntax Errors (MQ2xxx)
        [DiagnosticCode.MQ2001_UnexpectedToken] = "Unexpected token '{0}', expected '{1}'",
        [DiagnosticCode.MQ2002_MissingToken] = "Missing '{0}'",
        [DiagnosticCode.MQ2003_InvalidExpression] = "Invalid expression",
        [DiagnosticCode.MQ2004_MissingFromClause] = "Missing FROM clause",
        [DiagnosticCode.MQ2005_InvalidSelectList] = "Invalid SELECT list",
        [DiagnosticCode.MQ2006_MissingGroupByColumn] = "Column '{0}' must appear in GROUP BY clause",
        [DiagnosticCode.MQ2007_InvalidJoinCondition] = "Invalid JOIN condition",
        [DiagnosticCode.MQ2008_DuplicateAlias] = "Duplicate alias '{0}'",
        [DiagnosticCode.MQ2009_InvalidOrderByExpression] = "Invalid ORDER BY expression",
        [DiagnosticCode.MQ2010_MissingClosingParenthesis] = "Missing closing parenthesis ')'",
        [DiagnosticCode.MQ2011_MissingClosingBracket] = "Missing closing bracket ']' or '}}'",
        [DiagnosticCode.MQ2012_InvalidSchemaDefinition] = "Invalid schema definition",
        [DiagnosticCode.MQ2013_InvalidCTE] = "Invalid common table expression (CTE)",
        [DiagnosticCode.MQ2014_TrailingComma] = "Trailing comma",
        [DiagnosticCode.MQ2015_LeadingComma] = "Unexpected leading comma",
        [DiagnosticCode.MQ2016_IncompleteStatement] = "Incomplete statement",
        [DiagnosticCode.MQ2017_UnexpectedEndOfFile] = "Unexpected end of file",
        [DiagnosticCode.MQ2018_MissingOperator] = "Missing operator between expressions",
        [DiagnosticCode.MQ2019_InvalidOperator] = "Invalid operator '{0}'",
        [DiagnosticCode.MQ2020_MissingOperand] = "Missing operand for operator '{0}'",
        [DiagnosticCode.MQ2021_UnclosedFunctionCall] = "Unclosed function call '{0}'",
        [DiagnosticCode.MQ2022_InvalidAlias] = "Invalid alias '{0}'",
        [DiagnosticCode.MQ2023_MissingAsKeyword] = "Missing AS keyword before alias",
        [DiagnosticCode.MQ2024_InvalidSubquery] = "Invalid subquery",
        [DiagnosticCode.MQ2025_MissingSelectKeyword] = "Missing SELECT keyword",
        [DiagnosticCode.MQ2026_InvalidCaseExpression] = "Invalid CASE expression",
        [DiagnosticCode.MQ2027_MissingWhenClause] = "Missing WHEN clause in CASE expression",
        [DiagnosticCode.MQ2028_MissingThenClause] = "Missing THEN clause in CASE expression",
        [DiagnosticCode.MQ2029_MissingEndKeyword] = "Missing END keyword in CASE expression",

        // Semantic Errors (MQ3xxx)
        [DiagnosticCode.MQ3001_UnknownColumn] = "Unknown column '{0}'",
        [DiagnosticCode.MQ3002_AmbiguousColumn] = "Ambiguous column '{0}' - matches columns in '{1}' and '{2}'",
        [DiagnosticCode.MQ3003_UnknownTable] = "Unknown table or alias '{0}'",
        [DiagnosticCode.MQ3004_UnknownFunction] = "Unknown function '{0}'",
        [DiagnosticCode.MQ3005_TypeMismatch] = "Type mismatch: cannot convert '{0}' to '{1}'",
        [DiagnosticCode.MQ3006_InvalidArgumentCount] = "Function '{0}' expects {1} argument(s), but got {2}",
        [DiagnosticCode.MQ3007_InvalidOperandTypes] =
            "Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'",
        [DiagnosticCode.MQ3008_DivisionByZero] = "Division by zero",
        [DiagnosticCode.MQ3009_NullReference] = "Possible null reference",
        [DiagnosticCode.MQ3010_UnknownSchema] = "Unknown schema '{0}'",
        [DiagnosticCode.MQ3011_AggregateNotAllowed] = "Aggregate function '{0}' not allowed in this context",
        [DiagnosticCode.MQ3012_NonAggregateInSelect] =
            "Column '{0}' must appear in GROUP BY clause or be used in an aggregate function",
        [DiagnosticCode.MQ3013_CannotResolveMethod] = "Cannot resolve method '{0}' with the given argument types",
        [DiagnosticCode.MQ3014_InvalidPropertyAccess] = "'{0}' does not contain a property named '{1}'",
        [DiagnosticCode.MQ3015_UnknownAlias] = "Unknown alias '{0}'",
        [DiagnosticCode.MQ3016_CircularReference] = "Circular reference detected in '{0}'",
        [DiagnosticCode.MQ3031_SetOperatorMissingKeys] =
            "Set operator '{0}' must have key columns to determine how to combine rows",

        // Schema Definition Errors (MQ4xxx)
        [DiagnosticCode.MQ4001_InvalidBinarySchemaField] = "Invalid binary schema field '{0}'",
        [DiagnosticCode.MQ4002_InvalidTextSchemaField] = "Invalid text schema field '{0}'",
        [DiagnosticCode.MQ4003_UndefinedSchemaReference] = "Reference to undefined schema '{0}'",
        [DiagnosticCode.MQ4004_CircularSchemaReference] = "Circular schema reference: '{0}' references itself",
        [DiagnosticCode.MQ4005_InvalidEndianness] = "Invalid endianness specification",
        [DiagnosticCode.MQ4006_InvalidFieldConstraint] = "Invalid field constraint '{0}'",
        [DiagnosticCode.MQ4007_InvalidSchemaFieldType] = "Invalid type '{0}' in schema field",
        [DiagnosticCode.MQ4008_DuplicateSchemaField] = "Duplicate field name '{0}' in schema",
        [DiagnosticCode.MQ4009_InvalidSchemaName] = "Invalid schema name '{0}'",
        [DiagnosticCode.MQ4010_MissingRequiredField] = "Missing required field '{0}' in schema",

        // Warnings (MQ5xxx)
        [DiagnosticCode.MQ5001_UnusedAlias] = "Alias '{0}' is defined but never used",
        [DiagnosticCode.MQ5002_SelectStar] = "SELECT * used - consider specifying columns explicitly",
        [DiagnosticCode.MQ5003_ImplicitTypeConversion] = "Implicit conversion from '{0}' to '{1}'",
        [DiagnosticCode.MQ5004_PotentialNullReference] = "Potential null reference",
        [DiagnosticCode.MQ5005_RedundantParentheses] = "Redundant parentheses",
        [DiagnosticCode.MQ5006_DeprecatedSyntax] = "Deprecated syntax: {0}",
        [DiagnosticCode.MQ5007_PerformanceWarning] = "Performance warning: {0}",
        [DiagnosticCode.MQ5008_UnreachableCode] = "Unreachable code detected",

        // Unknown
        [DiagnosticCode.MQ9999_Unknown] = "An unknown error occurred: {0}"
    };

    /// <summary>
    ///     Gets the message template for a diagnostic code.
    /// </summary>
    public static string GetTemplate(DiagnosticCode code)
    {
        return MessageTemplates.TryGetValue(code, out var template)
            ? template
            : $"Error {code}";
    }

    /// <summary>
    ///     Gets a formatted message for a diagnostic code.
    /// </summary>
    public static string GetMessage(DiagnosticCode code, params object[] args)
    {
        var template = GetTemplate(code);

        try
        {
            return args.Length > 0 ? string.Format(template, args) : template;
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>
    ///     Gets the default severity for a diagnostic code.
    /// </summary>
    public static DiagnosticSeverity GetDefaultSeverity(DiagnosticCode code)
    {
        var codeValue = (int)code;

        return codeValue switch
        {
            >= 5000 and < 6000 => DiagnosticSeverity.Warning,
            >= 1000 and < 5000 => DiagnosticSeverity.Error,
            _ => DiagnosticSeverity.Error
        };
    }

    /// <summary>
    ///     Gets a human-readable category name for a diagnostic code.
    /// </summary>
    public static string GetCategory(DiagnosticCode code)
    {
        var codeValue = (int)code;

        return codeValue switch
        {
            >= 1000 and < 2000 => "Lexer",
            >= 2000 and < 3000 => "Syntax",
            >= 3000 and < 4000 => "Semantic",
            >= 4000 and < 5000 => "Schema",
            >= 5000 and < 6000 => "Warning",
            _ => "Unknown"
        };
    }

    /// <summary>
    ///     Generates a "did you mean?" suggestion using Levenshtein distance.
    /// </summary>
    public static string? GetDidYouMeanSuggestion(string input, IEnumerable<string> candidates, int maxDistance = 3)
    {
        string? bestMatch = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            var distance = ComputeLevenshteinDistance(input.ToLowerInvariant(), candidate.ToLowerInvariant());
            if (distance < bestDistance && distance <= maxDistance)
            {
                bestDistance = distance;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    private static int ComputeLevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);
        }

        return d[n, m];
    }
}
