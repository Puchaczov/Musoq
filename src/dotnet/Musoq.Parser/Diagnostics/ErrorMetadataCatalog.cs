using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Catalog of rich error metadata (Why / Try / Docs) for diagnostic codes.
///     Provides actionable guidance for each known error.
/// </summary>
public static class ErrorMetadataCatalog
{
    private static readonly Dictionary<DiagnosticCode, ErrorMetadata> Entries = new()
    {
        // ============================================
        // Lexer Errors (MQ1xxx) — Phase: Parse
        // ============================================
        [DiagnosticCode.MQ1001_UnknownToken] = new(
            DiagnosticCode.MQ1001_UnknownToken,
            DiagnosticPhase.Parse,
            "The lexer encountered a character that is not part of Musoq SQL syntax.",
            [
                "Remove or replace the unrecognized character.",
                "If this is a string literal, wrap it in single quotes: 'value'."
            ],
            "Core Spec §Lexical Structure"),

        [DiagnosticCode.MQ1002_UnterminatedString] = new(
            DiagnosticCode.MQ1002_UnterminatedString,
            DiagnosticPhase.Parse,
            "A string literal was opened with a single quote but never closed.",
            ["Add a closing single quote to the string literal."],
            "Core Spec §String Literals"),

        [DiagnosticCode.MQ1003_InvalidNumericLiteral] = new(
            DiagnosticCode.MQ1003_InvalidNumericLiteral,
            DiagnosticPhase.Parse,
            "The numeric literal format is not valid.",
            [
                "Check for misplaced decimal points or invalid digit characters.",
                "For hex use 0x prefix, for binary use 0b, for octal use 0o."
            ],
            "Core Spec §Numeric Literals"),

        [DiagnosticCode.MQ1004_InvalidEscapeSequence] = new(
            DiagnosticCode.MQ1004_InvalidEscapeSequence,
            DiagnosticPhase.Parse,
            "The string literal contains an escape sequence that Musoq does not recognize.",
            [
                "Use one of the supported escapes like \\n, \\r, \\t, \\', \\\\, \\uFFFF, or \\xFF.",
                "If you want a literal backslash, escape it as \\\\."
            ],
            "Core Spec §String Literals"),

        // ============================================
        // Parser/Syntax Errors (MQ2xxx) — Phase: Parse
        // ============================================
        [DiagnosticCode.MQ2001_UnexpectedToken] = new(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticPhase.Parse,
            "The parser encountered a token that does not fit the expected SQL grammar at this position.",
            [
                "Check for missing keywords, commas, or parentheses near this location.",
                "Verify the query follows Musoq SQL syntax."
            ],
            "Core Spec §Statement Structure"),

        [DiagnosticCode.MQ2002_MissingToken] = new(
            DiagnosticCode.MQ2002_MissingToken,
            DiagnosticPhase.Parse,
            "A required keyword, delimiter, or closing token is missing at this position.",
            [
                "Insert the missing keyword or delimiter near the highlighted location.",
                "Check for a missing FROM clause, comma, or closing parenthesis."
            ],
            "Core Spec §Statement Structure"),

        [DiagnosticCode.MQ2026_InvalidCaseExpression] = new(
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticPhase.Parse,
            "Musoq supports searched CASE only (CASE WHEN ... THEN ... END), not simple CASE (CASE expr WHEN value ...).",
            ["Rewrite as: CASE WHEN expr = value THEN result ELSE default END"],
            "Core Spec §CASE Expressions"),

        [DiagnosticCode.MQ2004_MissingFromClause] = new(
            DiagnosticCode.MQ2004_MissingFromClause,
            DiagnosticPhase.Parse,
            "Every SELECT query in Musoq requires a FROM clause specifying a data source.",
            [
                "Add a FROM clause: SELECT ... FROM #schema.method() alias",
                "For constant expressions, use: SELECT 1 FROM #system.dual() d"
            ],
            "Core Spec §FROM Clause"),

        [DiagnosticCode.MQ2013_InvalidCTE] = new(
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticPhase.Parse,
            "The Common Table Expression (CTE) syntax is invalid or incomplete.",
            [
                "Verify CTE format: WITH name AS (SELECT ...) SELECT ... FROM name",
                "Ensure the CTE body is a valid SELECT statement."
            ],
            "Core Spec §CTE"),

        [DiagnosticCode.MQ2016_IncompleteStatement] = new(
            DiagnosticCode.MQ2016_IncompleteStatement,
            DiagnosticPhase.Parse,
            "The query ended before Musoq could form a complete statement.",
            [
                "Complete the statement with the missing clause or expression.",
                "Start with a full query shape such as: SELECT ... FROM #schema.method() alias"
            ],
            "Core Spec §Statement Structure"),

        [DiagnosticCode.MQ2030_UnsupportedSyntax] = new(
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticPhase.Parse,
            "The query uses syntax that Musoq does not support or that is not valid in this position.",
            [
                "Rewrite the clause using Musoq SQL syntax.",
                "If this came from another SQL dialect, check the Musoq equivalent keywords."
            ],
            "Core Spec §Statement Structure"),

        // ============================================
        // Semantic Errors (MQ3xxx) — Phase: Bind
        // ============================================
        [DiagnosticCode.MQ3001_UnknownColumn] = new(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticPhase.Bind,
            "The column name could not be resolved in any of the available data sources.",
            [
                "Check the column name for typos.",
                "Qualify the column with a table alias: alias.ColumnName"
            ],
            "Core Spec §Column References"),

        [DiagnosticCode.MQ3002_AmbiguousColumn] = new(
            DiagnosticCode.MQ3002_AmbiguousColumn,
            DiagnosticPhase.Bind,
            "The column name matches columns in multiple data sources and is ambiguous.",
            ["Qualify the column with a table alias: alias.ColumnName"],
            "Core Spec §Column References"),

        [DiagnosticCode.MQ3003_UnknownTable] = new(
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticPhase.Bind,
            "The referenced table or schema method could not be resolved in the selected schema.",
            [
                "Check the method name after the schema prefix, for example #schema.method().",
                "Verify the schema exposes this data source."
            ],
            "Core Spec §FROM Clause"),

        [DiagnosticCode.MQ3004_UnknownFunction] = new(
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticPhase.Bind,
            "No function with this name and compatible arguments could be found.",
            [
                "Check the function name for typos.",
                "Verify the argument types match an available overload."
            ],
            "Core Spec §Functions"),

        [DiagnosticCode.MQ3007_InvalidOperandTypes] = new(
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticPhase.Bind,
            "The operator cannot be applied to the given operand types.",
            [
                "Convert operands to compatible types before comparing.",
                "For string date comparisons, parse to a numeric or date representation first."
            ],
            "Core Spec §Operator Type Rules"),

        [DiagnosticCode.MQ3012_NonAggregateInSelect] = new(
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            DiagnosticPhase.Bind,
            "Every selected column must be either aggregated or included in the GROUP BY clause.",
            [
                "Add the column to the GROUP BY clause.",
                "Wrap the column in an aggregate function (e.g., Count, Sum, Min, Max)."
            ],
            "Core Spec §GROUP BY and Aggregation"),

        [DiagnosticCode.MQ3022_MissingAlias] = new(
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticPhase.Bind,
            "In multi-source queries, method calls must be qualified with a source alias so Musoq can choose which schema library implementation to invoke.",
            [
                "Prefix the method with the owning source alias, for example: a.ToDecimal(a.Id) or b.Sum(b.Amount)",
                "For aggregates, remember that the alias chooses the schema library implementation, not the input column source",
                "If the aggregate is already aliased in SELECT, prefer that projection alias in ORDER BY instead of repeating the aggregate expression"
            ],
            "Core Spec §JOIN Clause / Function Calls in Multi-Source Queries and §GROUP BY and Aggregation"),

        [DiagnosticCode.MQ3021_DuplicateAlias] = new(
            DiagnosticCode.MQ3021_DuplicateAlias,
            DiagnosticPhase.Bind,
            "An alias with this name was already defined earlier in the query.",
            ["Use a different alias name for this source or expression."],
            "Core Spec §Aliasing"),

        [DiagnosticCode.MQ3010_UnknownSchema] = new(
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticPhase.Bind,
            "The referenced schema could not be found in the registered schema providers.",
            [
                "Verify the schema name is correct: #schemaName.method()",
                "Ensure the schema provider is registered."
            ],
            "Core Spec §Schema References"),

        [DiagnosticCode.MQ3030_ConstructionNotSupported] = new(
            DiagnosticCode.MQ3030_ConstructionNotSupported,
            DiagnosticPhase.Bind,
            "This syntax or construction is not supported in the current version of Musoq.",
            [
                "Rewrite using a supported equivalent (e.g., CASE WHEN for Coalesce with NULL).",
                "Check the documentation for supported constructions."
            ],
            "Core Spec §Unsupported Constructions"),

        [DiagnosticCode.MQ3029_UnresolvableMethod] = new(
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticPhase.Bind,
            "No method overload matches the provided argument types.",
            [
                "Check argument types and convert if necessary.",
                "Verify the method name is correct."
            ],
            "Core Spec §Method Resolution"),

        [DiagnosticCode.MQ3033_InterpretFunctionOutsideApply] = new(
            DiagnosticCode.MQ3033_InterpretFunctionOutsideApply,
            DiagnosticPhase.Bind,
            "Parse/Interpret functions can only be used inside CROSS APPLY or OUTER APPLY.",
            [
                "Move the function call to a CROSS APPLY or OUTER APPLY clause.",
                "Use TryParse in OUTER APPLY if parsing may fail."
            ],
            "Binary/Text Spec §Usage"),

        [DiagnosticCode.MQ3034_AmbiguousAggregateOwner] = new(
            DiagnosticCode.MQ3034_AmbiguousAggregateOwner,
            DiagnosticPhase.Bind,
            "An unqualified aggregate call matched multiple source aliases, but those aliases resolve to different aggregate implementations.",
            [
                "Prefix the aggregate with the intended source alias, for example: first.Sum(...) or second.Sum(...)",
                "Choose the alias whose schema library should own the aggregate implementation",
                "If the aggregate appears in ORDER BY, alias it in SELECT first and order by that projection alias"
            ],
            "Core Spec §JOIN Clause / Function Calls in Multi-Source Queries and §GROUP BY and Aggregation"),

        [DiagnosticCode.MQ3035_AmbiguousMethodOwner] = new(
            DiagnosticCode.MQ3035_AmbiguousMethodOwner,
            DiagnosticPhase.Bind,
            "An unqualified method call matched multiple source aliases, but those aliases resolve to different method implementations.",
            [
                "Prefix the method with the intended source alias, for example: first.MyMethod(...) or second.MyMethod(...)",
                "Choose the alias whose schema library should own the method implementation"
            ],
            "Core Spec §JOIN Clause / Function Calls in Multi-Source Queries"),

        [DiagnosticCode.MQ3005_TypeMismatch] = new(
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticPhase.Bind,
            "The expression type does not match the expected type in this context.",
            [
                "Use an explicit conversion function (e.g., ToInt32, ToDecimal, ToString).",
                "Verify the column type matches the expected usage."
            ],
            "Core Spec §Type System"),

        [DiagnosticCode.MQ3006_InvalidArgumentCount] = new(
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            DiagnosticPhase.Bind,
            "The function was called with the wrong number of arguments.",
            [
                "Check the function signature for the expected argument count.",
                "Verify you are calling the correct overload."
            ],
            "Core Spec §Functions"),

        [DiagnosticCode.MQ3008_DivisionByZero] = new(
            DiagnosticCode.MQ3008_DivisionByZero,
            DiagnosticPhase.Bind,
            "Division by zero detected in a constant expression.",
            ["Add a CASE WHEN check to guard against dividing by zero."],
            "Core Spec §Arithmetic Operators"),

        [DiagnosticCode.MQ3013_CannotResolveMethod] = new(
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticPhase.Bind,
            "No method overload matches the argument types provided.",
            [
                "Use explicit type conversions (ToInt32, ToString, etc.).",
                "Check the method name for typos."
            ],
            "Core Spec §Method Resolution"),

        [DiagnosticCode.MQ3014_InvalidPropertyAccess] = new(
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticPhase.Bind,
            "The referenced property does not exist on the object type.",
            [
                "Check the property name for typos.",
                "Verify the object type exposes this property."
            ],
            "Core Spec §Property Access"),

        [DiagnosticCode.MQ3028_UnknownProperty] = new(
            DiagnosticCode.MQ3028_UnknownProperty,
            DiagnosticPhase.Bind,
            "The property name could not be resolved on the referenced object.",
            [
                "Check the property name for typos.",
                "Verify the object type exposes this property before accessing it."
            ],
            "Core Spec §Property Access"),

        [DiagnosticCode.MQ3017_ObjectNotArray] = new(
            DiagnosticCode.MQ3017_ObjectNotArray,
            DiagnosticPhase.Bind,
            "Array access was attempted on an object that is not an array.",
            ["Verify the column or expression returns an array or IEnumerable type."],
            "Core Spec §Array Access"),

        [DiagnosticCode.MQ3018_NoIndexer] = new(
            DiagnosticCode.MQ3018_NoIndexer,
            DiagnosticPhase.Bind,
            "The object does not implement an indexer for bracket-access.",
            ["Use a different access pattern or check the object type."],
            "Core Spec §Indexer Access"),

        [DiagnosticCode.MQ3019_SetOperatorColumnCount] = new(
            DiagnosticCode.MQ3019_SetOperatorColumnCount,
            DiagnosticPhase.Bind,
            "Set operators (UNION, EXCEPT, INTERSECT) require both queries to have the same number of columns.",
            ["Adjust the SELECT lists so both queries produce the same number of columns."],
            "Core Spec §Set Operators"),

        [DiagnosticCode.MQ3020_SetOperatorColumnTypes] = new(
            DiagnosticCode.MQ3020_SetOperatorColumnTypes,
            DiagnosticPhase.Bind,
            "Set operators require matching column types between the two queries.",
            ["Convert columns to matching types using ToInt32, ToString, etc."],
            "Core Spec §Set Operators"),

        [DiagnosticCode.MQ3031_SetOperatorMissingKeys] = new(
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticPhase.Bind,
            "Musoq set operators do not infer row identity from the full projection. You must supply an explicit key-column list immediately after UNION, UNION ALL, EXCEPT, or INTERSECT.",
            [
                "Rewrite the query as UNION (<key_columns>), UNION ALL (<key_columns>), EXCEPT (<key_columns>), or INTERSECT (<key_columns>).",
                "Choose key columns that identify the rows Musoq should deduplicate, subtract, or match across both queries."
            ],
            "Core Spec §Set Operators"),

        [DiagnosticCode.MQ3023_TableNotDefined] = new(
            DiagnosticCode.MQ3023_TableNotDefined,
            DiagnosticPhase.Bind,
            "The referenced table or data source is not defined in this query.",
            [
                "Verify the table alias or CTE name is correct.",
                "Ensure the data source is defined in a FROM clause."
            ],
            "Core Spec §FROM Clause"),

        // ============================================
        // Schema/DataSource Errors (MQ4xxx) — Phase: DataSource
        // ============================================
        [DiagnosticCode.MQ4001_InvalidBinarySchemaField] = new(
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            DiagnosticPhase.DataSource,
            "A field in the binary schema definition is invalid.",
            ["Verify field type and constraints match the binary schema grammar."],
            "Binary/Text Spec §Binary Schema Fields"),

        [DiagnosticCode.MQ4003_UndefinedSchemaReference] = new(
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticPhase.DataSource,
            "The interpretation schema referenced by Interpret/Parse/InterpretAt was not defined.",
            [
                "Define the schema in a DEFINE SCHEMA block before referencing it.",
                "Verify the schema name matches exactly."
            ],
            "Binary/Text Spec §Schema References"),

        // ============================================
        // Feature-Gate Errors (MQ6xxx) — Phase: FeatureGate
        // ============================================
        [DiagnosticCode.MQ6001_CteUnavailable] = new(
            DiagnosticCode.MQ6001_CteUnavailable,
            DiagnosticPhase.FeatureGate,
            "CTE syntax (WITH ... AS ...) is currently unavailable in this parser path.",
            [
                "Rewrite as a single SELECT/GROUP BY query.",
                "Use source-level workaround (temporary file / staged query)."
            ],
            "Core Spec §CTE (availability note)"),

        [DiagnosticCode.MQ6002_DescUnavailable] = new(
            DiagnosticCode.MQ6002_DescUnavailable,
            DiagnosticPhase.FeatureGate,
            "DESC introspection is unavailable in this build due to alias-validator conflict.",
            ["Use schema probing workaround: SELECT * FROM #source(...) s TAKE 1"],
            "CLI Reference §desc (known issues)"),

        [DiagnosticCode.MQ6003_SimpleCaseNotSupported] = new(
            DiagnosticCode.MQ6003_SimpleCaseNotSupported,
            DiagnosticPhase.FeatureGate,
            "Simple CASE syntax (CASE expr WHEN value ...) is not supported. Musoq supports searched CASE only.",
            ["Rewrite as: CASE WHEN expr = value THEN result ELSE default END"],
            "Core Spec §CASE Expressions"),

        [DiagnosticCode.MQ6004_CoalesceWithLiteralNull] = new(
            DiagnosticCode.MQ6004_CoalesceWithLiteralNull,
            DiagnosticPhase.FeatureGate,
            "Coalesce/IfNull with literal NULL argument is not supported in this version.",
            ["Use: CASE WHEN x IS NULL THEN 'fallback' ELSE x END"],
            "Core Spec §Functions (null literal limitation)"),

        // ============================================
        // Runtime Errors (MQ7xxx) — Phase: Runtime
        // ============================================
        [DiagnosticCode.MQ7001_DataSourceBindingFailed] = new(
            DiagnosticCode.MQ7001_DataSourceBindingFailed,
            DiagnosticPhase.Runtime,
            "The runtime could not bind to the data source constructor.",
            [
                "Query source directly and cast columns inline (ToInt32/ToDecimal).",
                "Use a supported typed source path when available."
            ],
            "TABLE/COUPLE Spec §Integration (known limitations)"),

        [DiagnosticCode.MQ7002_DataSourceIteratorError] = new(
            DiagnosticCode.MQ7002_DataSourceIteratorError,
            DiagnosticPhase.Runtime,
            "The data source entered an invalid iterator state during execution.",
            [
                "Restart the Musoq server.",
                "Remove stale lock files if present."
            ],
            "Datasource Troubleshooting"),

        // ============================================
        // Warnings (MQ5xxx)
        // ============================================
        [DiagnosticCode.MQ5001_UnusedAlias] = new(
            DiagnosticCode.MQ5001_UnusedAlias,
            DiagnosticPhase.Bind,
            "An alias was defined but is never referenced in the query.",
            ["Remove the unused alias or reference it in the query."],
            "Core Spec §Aliasing"),

        [DiagnosticCode.MQ5003_ImplicitTypeConversion] = new(
            DiagnosticCode.MQ5003_ImplicitTypeConversion,
            DiagnosticPhase.Bind,
            "An implicit type conversion is occurring which may cause unexpected results.",
            ["Use an explicit conversion function to make the intent clear."],
            "Core Spec §Type System"),

        [DiagnosticCode.MQ5009_OrderByAliasBehavior] = new(
            DiagnosticCode.MQ5009_OrderByAliasBehavior,
            DiagnosticPhase.Bind,
            "ORDER BY alias may not resolve to the computed expression in this version.",
            ["Repeat the expression explicitly in ORDER BY: ORDER BY ToInt32(e.Salary) DESC"],
            "Core Spec §ORDER BY (alias behavior note)"),

        // ============================================
        // Code Generation Errors (MQ8xxx) — Phase: Runtime
        // ============================================
        [DiagnosticCode.MQ8001_CodeGenerationFailed] = new(
            DiagnosticCode.MQ8001_CodeGenerationFailed,
            DiagnosticPhase.Runtime,
            "The Roslyn compilation of the internally generated C# code failed. This is typically an internal engine issue, not a user error.",
            [
                "Report the query to the Musoq issue tracker.",
                "Try simplifying the query to narrow down the trigger."
            ],
            "Architecture §Code Generation")
    };

    /// <summary>
    ///     Retrieves error metadata for a diagnostic code, or null if not found.
    /// </summary>
    public static ErrorMetadata? Get(DiagnosticCode code)
    {
        return Entries.GetValueOrDefault(code);
    }
}
