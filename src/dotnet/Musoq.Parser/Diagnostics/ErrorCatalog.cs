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
        [DiagnosticCode.MQ1009_NumericLiteralOutOfRange] = "Numeric literal '{0}' is outside the supported range",

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
        [DiagnosticCode.MQ2035_MissingRequiredAlias] = "A source requires an alias in this query block",
        [DiagnosticCode.MQ2026_InvalidCaseExpression] = "Invalid CASE expression",
        [DiagnosticCode.MQ2027_MissingWhenClause] = "Missing WHEN clause in CASE expression",
        [DiagnosticCode.MQ2028_MissingThenClause] = "Missing THEN clause in CASE expression",
        [DiagnosticCode.MQ2029_MissingEndKeyword] = "Missing END keyword in CASE expression",
        [DiagnosticCode.MQ2036_MultipleExecutableStatements] = "Multiple executable statements are not supported by this compilation entry point",
        [DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed] = "The {0} predicate list cannot be empty",
        [DiagnosticCode.MQ2038_InvalidSliceCount] = "{0} count must be a non-negative integer",
        [DiagnosticCode.MQ2039_TieBreakRequiresAsOfJoin] = "TIE BREAK BY requires an ASOF JOIN",
        [DiagnosticCode.MQ2040_InvalidDiagnosticCommand] = "Invalid diagnostic command",
        [DiagnosticCode.MQ2041_InvalidStarModifierOrder] = "Star modifiers are out of order or duplicated",

        // Semantic Errors (MQ3xxx)
        [DiagnosticCode.MQ3001_UnknownColumn] = "Unknown column '{0}'",
        [DiagnosticCode.MQ3002_AmbiguousColumn] = "Ambiguous column '{0}' - matches columns in '{1}' and '{2}'",
        [DiagnosticCode.MQ3005_TypeMismatch] = "Type mismatch: cannot convert '{0}' to '{1}'",
        [DiagnosticCode.MQ3007_InvalidOperandTypes] =
            "Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'",
        [DiagnosticCode.MQ3008_DivisionByZero] = "Division by zero",
        [DiagnosticCode.MQ3010_UnknownSchema] = "Unknown schema '{0}'",
        [DiagnosticCode.MQ3011_AggregateNotAllowed] = "Aggregate function '{0}' not allowed in this context",
        [DiagnosticCode.MQ3012_NonAggregateInSelect] =
            "Column '{0}' must appear in GROUP BY clause or be used in an aggregate function",
        [DiagnosticCode.MQ3015_UnknownAlias] = "Unknown alias '{0}'",
        [DiagnosticCode.MQ3016_CircularReference] = "Circular reference detected in '{0}'",
        [DiagnosticCode.MQ3022_MissingAlias] =
            "Method call '{0}' must be qualified with a source alias when more than one schema is used",
        [DiagnosticCode.MQ3034_AmbiguousAggregateOwner] =
            "Aggregate call '{0}' is ambiguous because multiple source aliases expose different implementations: {1}",
        [DiagnosticCode.MQ3035_AmbiguousMethodOwner] =
            "Method call '{0}' is ambiguous because multiple source aliases expose different implementations: {1}",
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
        [DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata] = "Named datasource arguments require reflected source metadata for '{0}'.",
        [DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection] = "Source entity '{0}' for '{1}.{2}' cannot be emitted as generated execution code: {3}",
        [DiagnosticCode.MQ3085_UnknownSource] = "Source '{0}' does not exist in schema '{1}'",
        [DiagnosticCode.MQ3086_UnknownCallable] = "Unknown callable '{0}'",
        [DiagnosticCode.MQ3087_InvalidCallableArity] = "Callable '{0}' does not accept {1} argument(s); expected {2}",
        [DiagnosticCode.MQ3088_NoMatchingCallableOverload] = "No overload of callable '{0}' accepts argument types ({1})",
        [DiagnosticCode.MQ3089_AmbiguousCallableOverload] = "Callable '{0}' is ambiguous for argument types ({1})",
        [DiagnosticCode.MQ3090_UnsupportedCastTarget] = "Cast target '{0}' is not supported",
        [DiagnosticCode.MQ3091_InvalidConstantCast] = "Constant value cannot be cast to '{0}': {1}",
        [DiagnosticCode.MQ3092_AggregateInGroupBy] = "GROUP BY expressions cannot contain aggregate functions",
        [DiagnosticCode.MQ3093_OrderByOrdinalUnsupported] = "ORDER BY numeric positions are not supported",
        [DiagnosticCode.MQ3094_InvalidConstantRegex] = "The constant regex pattern is invalid: {0}",
        [DiagnosticCode.MQ3095_ScalarSubqueryCardinality] = "Scalar subquery may return more than one row",
        [DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess] = "Variable key access is not supported",
        [DiagnosticCode.MQ3097_UnsupportedAggregateProjection] = "This aggregate projection shape is not supported without GROUP BY",
        [DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey] = "Bounded RANGE frames require exactly one numeric ORDER BY key",

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
        [DiagnosticCode.MQ4016_UnsupportedSchemaConstruction] =
            "This interpretation schema construction is not supported by the code generator",

        // Warnings (MQ5xxx)
        [DiagnosticCode.MQ5003_ImplicitTypeConversion] = "Ambiguous date text is implicitly converted from '{0}' to '{1}'",
        [DiagnosticCode.MQ5008_UnreachableCode] = "Unreachable code detected",
        [DiagnosticCode.MQ5013_SourceContractWarning] = "Source contract warning: {0}",
        [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape] =
            "Ordinary string escape '{0}' changes path-like text; use a raw literal or double the backslash if the text should be preserved.",

        // Runtime Errors (MQ7xxx)
        [DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded] =
            "Recursive CTE iteration limit of {0} was exceeded.",
        [DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded] =
            "Recursive CTE row limit of {0} was exceeded.",
        [DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded] =
            "Recursive CTE invariant snapshot row limit of {0} was exceeded.",
        [DiagnosticCode.MQ7010_DataSourceOpenFailed] =
            "The data source could not be opened for schema '{0}', source '{1}', alias '{2}'.",
        [DiagnosticCode.MQ7011_DataSourceReadFailed] =
            "The data source failed while reading rows for schema '{0}', source '{1}', alias '{2}'.",
        [DiagnosticCode.MQ7012_DataSourceCleanupFailed] =
            "The data source failed while cleaning up rows for schema '{0}', source '{1}', alias '{2}'.",
        // Code Generation Errors (MQ8xxx)
        [DiagnosticCode.MQ8001_CodeGenerationFailed] =
            "Generated C# code failed to compile: {0}",

        // Warnings (MQ5xxx)
        [DiagnosticCode.MQ5015_SuspiciousRegexEscape] = "An ordinary string escape changes a regex token '{0}'",
        [DiagnosticCode.MQ5016_GlobWildcardInLike] = "Glob wildcard '{0}' is used with SQL LIKE",
        [DiagnosticCode.MQ5017_NullComparison] = "Comparison with NULL using '{0}' is always UNKNOWN",
        [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck] = "IS NULL on optional alias '{0}.{1}' cannot distinguish a missing outer-join row from a present NULL value",
        [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter] = "WHERE predicate rejects NULL-extended rows from optional alias '{0}' and effectively turns the outer join into an inner join",
        [DiagnosticCode.MQ5020_SetOperationOrderByScope] = "Set-operation ORDER BY scope migration advisory (compatibility only)",
        [DiagnosticCode.MQ5021_UnorderedSkip] = "SKIP {0} is used without ORDER BY, so the skipped rows are not deterministic",
        [DiagnosticCode.MQ5022_UnusedCte] = "CTE '{0}' is not reachable from the outer query",
        [DiagnosticCode.MQ5023_UnusedScriptVariable] = "Script variable '{0}' is not transitively used",
        [DiagnosticCode.MQ5024_NullSensitiveNotIn] = "NOT IN contains NULL, so non-matching values evaluate to UNKNOWN",
        [DiagnosticCode.MQ5025_ImpossibleImplicitConversion] = "This constant cannot be implicitly converted to {0}; the comparison cannot match",
        [DiagnosticCode.MQ5026_SetOperationSliceScope] = "Set-operation SKIP/TAKE scope migration advisory (compatibility only)",

        // Internal
        [DiagnosticCode.MQ9001_InternalCompilerError] = "The compiler encountered an internal failure. Reference '{0}' when reporting this issue.",
        [DiagnosticCode.MQ9002_InternalExecutionError] = "The query encountered an internal execution failure. Reference '{0}' when reporting this issue.",

    }.ToFrozenDictionary();

    /// <summary>
    ///     Gets the message template for a diagnostic code.
    /// </summary>
    public static string GetTemplate(DiagnosticCode code)
    {
        return DiagnosticDescriptorRegistry.Get(code)?.MessageTemplate ?? $"Error {code}";
    }

    internal static bool HasTemplate(DiagnosticCode code)
    {
        return MessageTemplates.ContainsKey(code);
    }

    internal static string GetTemplateLegacy(DiagnosticCode code)
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
        return DiagnosticDescriptorRegistry.Get(code)?.DefaultSeverity ?? GetDefaultSeverityLegacy(code);
    }

    internal static DiagnosticSeverity GetDefaultSeverityLegacy(DiagnosticCode code)
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
        return DiagnosticDescriptorRegistry.Get(code)?.Category ?? GetCategoryLegacy(code);
    }

    internal static string GetCategoryLegacy(DiagnosticCode code)
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
