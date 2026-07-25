using System.Collections.Frozen;
using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Catalog of error messages for all diagnostic codes.
///     Provides localized and formatted error messages.
/// </summary>
public static class ErrorCatalog
{
    private static readonly FrozenDictionary<DiagnosticCode, string> MessageTemplates = new Dictionary<DiagnosticCode, string>()
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
        [DiagnosticCode.MQ2034_InvalidNamedSourceArgument] = "Invalid named datasource argument: {0}",
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
        [DiagnosticCode.MQ3022_MissingAlias] =
            "Method call '{0}' must be qualified with a source alias when more than one schema is used",
        [DiagnosticCode.MQ3034_AmbiguousAggregateOwner] =
            "Aggregate call '{0}' is ambiguous because multiple source aliases expose different implementations: {1}",
        [DiagnosticCode.MQ3035_AmbiguousMethodOwner] =
            "Method call '{0}' is ambiguous because multiple source aliases expose different implementations: {1}",
        [DiagnosticCode.MQ3031_SetOperatorMissingKeys] =
            "Legacy set-operator missing-key diagnostic; omitted keys now compare all projected values, and explicit keys are optional",
        [DiagnosticCode.MQ3036_AsOfJoinMissingInequality] =
            "ASOF JOIN requires at least one inequality condition (>=, >, <=, <).",
        [DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities] =
            "ASOF JOIN supports exactly one inequality condition. Found {0}.",
        [DiagnosticCode.MQ3038_AsOfJoinOrNotSupported] =
            "ASOF JOIN ON clause does not support OR.",
        [DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides] =
            "ASOF JOIN inequality must reference columns from both sides.",
        [DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable] =
            "ASOF JOIN inequality column type '{0}' is not orderable.",
        [DiagnosticCode.MQ3055_InvalidValuesSource] = "Invalid VALUES source: {0}",
        [DiagnosticCode.MQ3068_StarRenameDuplicateSource] = "Duplicate source column '{0}' in RENAME list.",
        [DiagnosticCode.MQ3069_StarRenameDuplicateTarget] = "Duplicate target column '{0}' in RENAME list.",
        [DiagnosticCode.MQ3070_StarRenameColumnNotFound] = "RENAME references non-existent output column '{0}'.",
        [DiagnosticCode.MQ3071_SourceContractError] = "Source contract error: {0}",
        [DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword] = "Recursive CTE '{0}' requires WITH RECURSIVE.",
        [DiagnosticCode.MQ3073_InvalidRecursiveCteShape] = "Invalid recursive CTE shape: {0}",
        [DiagnosticCode.MQ3074_InvalidRecursiveCteReference] = "Invalid recursive CTE reference: {0}",
        [DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator] = "Operator '{0}' is not supported in a recursive CTE member.",
        [DiagnosticCode.MQ3076_RecursiveCteOutputMismatch] = "Recursive CTE output mismatch: {0}",
        [DiagnosticCode.MQ3077_CteColumnListCountMismatch] =
            "CTE '{0}' declares {1} column name(s), but its query projects {2} column(s).",
        [DiagnosticCode.MQ3078_DuplicateCteColumnName] = "CTE '{0}' declares duplicate column name '{1}'.",
        [DiagnosticCode.MQ3079_UnknownSourceArgument] = "Datasource argument '{0}' is not present in the selected source signature.",
        [DiagnosticCode.MQ3080_DuplicateSourceArgument] = "Datasource argument '{0}' was supplied more than once.",
        [DiagnosticCode.MQ3081_MissingRequiredSourceArgument] = "Required datasource argument '{0}' was not supplied.",
        [DiagnosticCode.MQ3082_AmbiguousSourceInvocation] = "Datasource invocation is ambiguous: {0}",
        [DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata] = "Named datasource arguments require reflected source metadata for '{0}'.",

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
        [DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField] =
            "Switch selector '{0}' must reference a field declared before the switch field",
        [DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias] = "Duplicate switch branch alias '{0}'",
        [DiagnosticCode.MQ4013_InvalidSwitchCaseLabel] =
            "Switch case label must be a constant scalar literal",
        [DiagnosticCode.MQ4014_InvalidSubstreamModifier] =
            "Substream requires a 'raw' or 'as <type>' modifier with an optional 'exact' or 'lax' mode",
        [DiagnosticCode.MQ4015_InvalidSubstreamTarget] =
            "Substream 'as' requires a valid target type",

        // Warnings (MQ5xxx)
        [DiagnosticCode.MQ5001_UnusedAlias] = "Alias '{0}' is defined but never used",
        [DiagnosticCode.MQ5002_SelectStar] = "SELECT * used - consider specifying columns explicitly",
        [DiagnosticCode.MQ5003_ImplicitTypeConversion] = "Implicit conversion from '{0}' to '{1}'",
        [DiagnosticCode.MQ5004_PotentialNullReference] = "Potential null reference",
        [DiagnosticCode.MQ5005_RedundantParentheses] = "Redundant parentheses",
        [DiagnosticCode.MQ5006_DeprecatedSyntax] = "Deprecated syntax: {0}",
        [DiagnosticCode.MQ5007_PerformanceWarning] = "Performance warning: {0}",
        [DiagnosticCode.MQ5008_UnreachableCode] = "Unreachable code detected",
        [DiagnosticCode.MQ5009_OrderByAliasBehavior] =
            "ORDER BY alias '{0}' may not resolve to the computed expression in this version",
        [DiagnosticCode.MQ5012_OptimizationFallback] = "Optimization fallback: {0}",
        [DiagnosticCode.MQ5013_SourceContractWarning] = "Source contract warning: {0}",

        // Feature-Gate Errors (MQ6xxx)
        [DiagnosticCode.MQ6001_CteUnavailable] =
            "CTE syntax (WITH ... AS ...) is currently unavailable in this parser path",
        [DiagnosticCode.MQ6002_DescUnavailable] =
            "DESC introspection is unavailable in this build",
        [DiagnosticCode.MQ6003_SimpleCaseNotSupported] =
            "Simple CASE syntax is not supported; use searched CASE (CASE WHEN ... THEN ... END)",
        [DiagnosticCode.MQ6004_CoalesceWithLiteralNull] =
            "Coalesce/IfNull with literal NULL is not supported in this version",

        // Runtime Errors (MQ7xxx)
        [DiagnosticCode.MQ7001_DataSourceBindingFailed] =
            "Could not bind to data source constructor for '{0}'",
        [DiagnosticCode.MQ7002_DataSourceIteratorError] =
            "Data source entered invalid iterator state",
        [DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded] =
            "Recursive CTE iteration limit of {0} was exceeded.",
        [DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded] =
            "Recursive CTE row limit of {0} was exceeded.",
        [DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded] =
            "Recursive CTE invariant snapshot row limit of {0} was exceeded.",

        // Code Generation Errors (MQ8xxx)
        [DiagnosticCode.MQ8001_CodeGenerationFailed] =
            "Generated C# code failed to compile: {0}",

        // Unknown
        [DiagnosticCode.MQ9999_Unknown] = "An unknown error occurred: {0}"
    }.ToFrozenDictionary();

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
        ArgumentNullException.ThrowIfNull(args);
        var template = GetTemplate(code);

        try
        {
            return args.Length > 0 ? string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args) : template;
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
            >= 6000 and < 9000 => DiagnosticSeverity.Error,
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
            >= 6000 and < 7000 => "FeatureGate",
            >= 7000 and < 8000 => "Runtime",
            >= 8000 and < 9000 => "CodeGeneration",
            _ => "Unknown"
        };
    }

    /// <summary>
    ///     Generates a "did you mean?" suggestion using Levenshtein distance.
    /// </summary>
    public static string? GetDidYouMeanSuggestion(string input, IEnumerable<string> candidates, int maxDistance = 3)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(candidates);
        string? bestMatch = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            var distance = ComputeLevenshteinDistance(input.ToUpperInvariant(), candidate.ToUpperInvariant());
            if (distance < bestDistance && distance <= maxDistance)
            {
                bestDistance = distance;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    private static int ComputeLevenshteinDistance(string source, string candidate)
    {
        var sourceLength = source.Length;
        var candidateLength = candidate.Length;
        var distances = new int[sourceLength + 1][];

        if (sourceLength == 0) return candidateLength;
        if (candidateLength == 0) return sourceLength;

        for (var i = 0; i <= sourceLength; i++)
            distances[i] = new int[candidateLength + 1];

        for (var i = 0; i <= sourceLength; i++) distances[i][0] = i;
        for (var j = 0; j <= candidateLength; j++) distances[0][j] = j;

        for (var i = 1; i <= sourceLength; i++)
        for (var j = 1; j <= candidateLength; j++)
        {
            var cost = source[i - 1] == candidate[j - 1] ? 0 : 1;
            distances[i][j] = Math.Min(
                Math.Min(distances[i - 1][j] + 1, distances[i][j - 1] + 1),
                distances[i - 1][j - 1] + cost);
        }

        return distances[sourceLength][candidateLength];
    }
}
