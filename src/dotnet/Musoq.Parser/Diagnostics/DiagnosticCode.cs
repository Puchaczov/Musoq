namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enumeration of all diagnostic codes used in Musoq.
///     Codes are organized by category:
///     - MQ1xxx: Lexer errors
///     - MQ2xxx: Parser/Syntax errors
///     - MQ3xxx: Semantic errors
///     - MQ4xxx: Schema definition errors
///     - MQ5xxx: Warnings
/// </summary>
public enum DiagnosticCode
{
    // ============================================
    // Lexer Errors (MQ1xxx)
    // ============================================

    /// <summary>
    ///     An unrecognized token was encountered.
    /// </summary>
    MQ1001_UnknownToken = 1001,

    /// <summary>
    ///     A string literal was not properly terminated.
    /// </summary>
    MQ1002_UnterminatedString = 1002,

    /// <summary>
    ///     An invalid numeric literal format.
    /// </summary>
    MQ1003_InvalidNumericLiteral = 1003,

    /// <summary>
    ///     An invalid escape sequence in a string.
    /// </summary>
    MQ1004_InvalidEscapeSequence = 1004,

    /// <summary>
    ///     A block comment was not properly terminated.
    /// </summary>
    MQ1005_UnterminatedBlockComment = 1005,

    /// <summary>
    ///     Invalid hexadecimal number format.
    /// </summary>
    MQ1006_InvalidHexNumber = 1006,

    /// <summary>
    ///     Invalid binary number format.
    /// </summary>
    MQ1007_InvalidBinaryNumber = 1007,

    /// <summary>
    ///     Invalid octal number format.
    /// </summary>
    MQ1008_InvalidOctalNumber = 1008,

    // ============================================
    // Parser/Syntax Errors (MQ2xxx)
    // ============================================

    /// <summary>
    ///     An unexpected token was encountered.
    /// </summary>
    MQ2001_UnexpectedToken = 2001,

    /// <summary>
    ///     A required token is missing.
    /// </summary>
    MQ2002_MissingToken = 2002,

    /// <summary>
    ///     An invalid expression was encountered.
    /// </summary>
    MQ2003_InvalidExpression = 2003,

    /// <summary>
    ///     The FROM clause is missing.
    /// </summary>
    MQ2004_MissingFromClause = 2004,

    /// <summary>
    ///     The SELECT list is invalid or empty.
    /// </summary>
    MQ2005_InvalidSelectList = 2005,

    /// <summary>
    ///     A column is missing in GROUP BY clause.
    /// </summary>
    MQ2006_MissingGroupByColumn = 2006,

    /// <summary>
    ///     The JOIN condition is invalid.
    /// </summary>
    MQ2007_InvalidJoinCondition = 2007,

    /// <summary>
    ///     A duplicate alias was found.
    /// </summary>
    MQ2008_DuplicateAlias = 2008,

    /// <summary>
    ///     The ORDER BY expression is invalid.
    /// </summary>
    MQ2009_InvalidOrderByExpression = 2009,

    /// <summary>
    ///     A closing parenthesis is missing.
    /// </summary>
    MQ2010_MissingClosingParenthesis = 2010,

    /// <summary>
    ///     A closing bracket is missing.
    /// </summary>
    MQ2011_MissingClosingBracket = 2011,

    /// <summary>
    ///     The schema definition is invalid.
    /// </summary>
    MQ2012_InvalidSchemaDefinition = 2012,

    /// <summary>
    ///     The CTE (Common Table Expression) is invalid.
    /// </summary>
    MQ2013_InvalidCTE = 2013,

    /// <summary>
    ///     A trailing comma was found.
    /// </summary>
    MQ2014_TrailingComma = 2014,

    /// <summary>
    ///     A leading comma was found where not expected.
    /// </summary>
    MQ2015_LeadingComma = 2015,

    /// <summary>
    ///     The statement is incomplete.
    /// </summary>
    MQ2016_IncompleteStatement = 2016,

    /// <summary>
    ///     An unexpected end of file was encountered.
    /// </summary>
    MQ2017_UnexpectedEndOfFile = 2017,

    /// <summary>
    ///     Missing operator between expressions.
    /// </summary>
    MQ2018_MissingOperator = 2018,

    /// <summary>
    ///     Invalid operator usage.
    /// </summary>
    MQ2019_InvalidOperator = 2019,

    /// <summary>
    ///     Missing operand for operator.
    /// </summary>
    MQ2020_MissingOperand = 2020,

    /// <summary>
    ///     Unclosed function call.
    /// </summary>
    MQ2021_UnclosedFunctionCall = 2021,

    /// <summary>
    ///     Invalid alias syntax.
    /// </summary>
    MQ2022_InvalidAlias = 2022,

    /// <summary>
    ///     Missing AS keyword (when required).
    /// </summary>
    MQ2023_MissingAsKeyword = 2023,

    /// <summary>
    ///     Invalid subquery.
    /// </summary>
    MQ2024_InvalidSubquery = 2024,

    /// <summary>
    ///     Missing SELECT keyword.
    /// </summary>
    MQ2025_MissingSelectKeyword = 2025,

    /// <summary>
    ///     Invalid CASE expression.
    /// </summary>
    MQ2026_InvalidCaseExpression = 2026,

    /// <summary>
    ///     Missing WHEN clause in CASE.
    /// </summary>
    MQ2027_MissingWhenClause = 2027,

    /// <summary>
    ///     Missing THEN clause in CASE.
    /// </summary>
    MQ2028_MissingThenClause = 2028,

    /// <summary>
    ///     Missing END keyword in CASE.
    /// </summary>
    MQ2029_MissingEndKeyword = 2029,

    /// <summary>
    ///     Unsupported syntax or feature.
    /// </summary>
    MQ2030_UnsupportedSyntax = 2030,

    // ============================================
    // Semantic Errors (MQ3xxx)
    // ============================================

    /// <summary>
    ///     Reference to an unknown column.
    /// </summary>
    MQ3001_UnknownColumn = 3001,

    /// <summary>
    ///     Ambiguous column reference.
    /// </summary>
    MQ3002_AmbiguousColumn = 3002,

    /// <summary>
    ///     Reference to an unknown table or alias.
    /// </summary>
    MQ3003_UnknownTable = 3003,

    /// <summary>
    ///     Reference to an unknown function.
    /// </summary>
    MQ3004_UnknownFunction = 3004,

    /// <summary>
    ///     Type mismatch in expression.
    /// </summary>
    MQ3005_TypeMismatch = 3005,

    /// <summary>
    ///     Invalid number of arguments for function.
    /// </summary>
    MQ3006_InvalidArgumentCount = 3006,

    /// <summary>
    ///     Invalid operand types for operator.
    /// </summary>
    MQ3007_InvalidOperandTypes = 3007,

    /// <summary>
    ///     Division by zero.
    /// </summary>
    MQ3008_DivisionByZero = 3008,

    /// <summary>
    ///     Potential null reference.
    /// </summary>
    MQ3009_NullReference = 3009,

    /// <summary>
    ///     Unknown schema reference.
    /// </summary>
    MQ3010_UnknownSchema = 3010,

    /// <summary>
    ///     Aggregate function not allowed in this context.
    /// </summary>
    MQ3011_AggregateNotAllowed = 3011,

    /// <summary>
    ///     Non-aggregate expression in SELECT with GROUP BY.
    /// </summary>
    MQ3012_NonAggregateInSelect = 3012,

    /// <summary>
    ///     Cannot resolve method overload.
    /// </summary>
    MQ3013_CannotResolveMethod = 3013,

    /// <summary>
    ///     Invalid property access.
    /// </summary>
    MQ3014_InvalidPropertyAccess = 3014,

    /// <summary>
    ///     Unknown alias reference.
    /// </summary>
    MQ3015_UnknownAlias = 3015,

    /// <summary>
    ///     Circular reference detected.
    /// </summary>
    MQ3016_CircularReference = 3016,

    /// <summary>
    ///     Object is not an array when array access was attempted.
    /// </summary>
    MQ3017_ObjectNotArray = 3017,

    /// <summary>
    ///     Object does not implement an indexer.
    /// </summary>
    MQ3018_NoIndexer = 3018,

    /// <summary>
    ///     Set operator (UNION, EXCEPT, INTERSECT) has mismatched column count.
    /// </summary>
    MQ3019_SetOperatorColumnCount = 3019,

    /// <summary>
    ///     Set operator (UNION, EXCEPT, INTERSECT) has mismatched column types.
    /// </summary>
    MQ3020_SetOperatorColumnTypes = 3020,

    /// <summary>
    ///     Duplicate alias - an alias with this name was already defined.
    /// </summary>
    MQ3021_DuplicateAlias = 3021,

    /// <summary>
    ///     Required alias is missing.
    /// </summary>
    MQ3022_MissingAlias = 3022,

    /// <summary>
    ///     Table or data source is not defined.
    /// </summary>
    MQ3023_TableNotDefined = 3023,

    /// <summary>
    ///     GROUP BY index is out of range.
    /// </summary>
    MQ3024_GroupByIndexOutOfRange = 3024,

    /// <summary>
    ///     Column must be an array or implement IEnumerable.
    /// </summary>
    MQ3025_ColumnMustBeArray = 3025,

    /// <summary>
    ///     Column must be marked as bindable property as table.
    /// </summary>
    MQ3026_ColumnNotBindable = 3026,

    /// <summary>
    ///     Invalid query expression type - expression returns unexpected type.
    /// </summary>
    MQ3027_InvalidExpressionType = 3027,

    /// <summary>
    ///     Unknown property on object.
    /// </summary>
    MQ3028_UnknownProperty = 3028,

    /// <summary>
    ///     Method cannot be resolved with the given arguments.
    /// </summary>
    MQ3029_UnresolvableMethod = 3029,

    /// <summary>
    ///     Construction or syntax is not yet supported.
    /// </summary>
    MQ3030_ConstructionNotSupported = 3030,

    /// <summary>
    ///     Set operator (UNION, EXCEPT, INTERSECT) is missing required key columns.
    /// </summary>
    MQ3031_SetOperatorMissingKeys = 3031,

    // ============================================
    // Schema Definition Errors (MQ4xxx)
    // ============================================

    /// <summary>
    ///     Invalid binary schema field definition.
    /// </summary>
    MQ4001_InvalidBinarySchemaField = 4001,

    /// <summary>
    ///     Invalid text schema field definition.
    /// </summary>
    MQ4002_InvalidTextSchemaField = 4002,

    /// <summary>
    ///     Reference to an undefined schema.
    /// </summary>
    MQ4003_UndefinedSchemaReference = 4003,

    /// <summary>
    ///     Circular schema reference detected.
    /// </summary>
    MQ4004_CircularSchemaReference = 4004,

    /// <summary>
    ///     Invalid endianness specification.
    /// </summary>
    MQ4005_InvalidEndianness = 4005,

    /// <summary>
    ///     Invalid field constraint.
    /// </summary>
    MQ4006_InvalidFieldConstraint = 4006,

    /// <summary>
    ///     Invalid type in schema field.
    /// </summary>
    MQ4007_InvalidSchemaFieldType = 4007,

    /// <summary>
    ///     Duplicate field name in schema.
    /// </summary>
    MQ4008_DuplicateSchemaField = 4008,

    /// <summary>
    ///     Invalid schema name.
    /// </summary>
    MQ4009_InvalidSchemaName = 4009,

    /// <summary>
    ///     Missing required field in schema.
    /// </summary>
    MQ4010_MissingRequiredField = 4010,

    // ============================================
    // Warnings (MQ5xxx)
    // ============================================

    /// <summary>
    ///     An alias is defined but never used.
    /// </summary>
    MQ5001_UnusedAlias = 5001,

    /// <summary>
    ///     SELECT * is used (consider explicit columns).
    /// </summary>
    MQ5002_SelectStar = 5002,

    /// <summary>
    ///     Implicit type conversion occurring.
    /// </summary>
    MQ5003_ImplicitTypeConversion = 5003,

    /// <summary>
    ///     Potential null reference in expression.
    /// </summary>
    MQ5004_PotentialNullReference = 5004,

    /// <summary>
    ///     Redundant parentheses.
    /// </summary>
    MQ5005_RedundantParentheses = 5005,

    /// <summary>
    ///     Deprecated syntax used.
    /// </summary>
    MQ5006_DeprecatedSyntax = 5006,

    /// <summary>
    ///     Performance warning.
    /// </summary>
    MQ5007_PerformanceWarning = 5007,

    /// <summary>
    ///     Unreachable code detected.
    /// </summary>
    MQ5008_UnreachableCode = 5008,

    // ============================================
    // Internal/Unknown (MQ9xxx)
    // ============================================

    /// <summary>
    ///     Unknown or unclassified error.
    /// </summary>
    MQ9999_Unknown = 9999
}
