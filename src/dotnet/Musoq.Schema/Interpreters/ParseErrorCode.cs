namespace Musoq.Schema.Interpreters;

/// <summary>
///     Error codes for interpretation schema parsing failures.
///     All codes follow the ISExxxx format (Interpretation Schema Error).
/// </summary>
public enum ParseErrorCode
{
    /// <summary>
    ///     ISE001: Insufficient data to complete parsing.
    /// </summary>
    InsufficientData = 1,

    /// <summary>
    ///     ISE002: Validation constraint failed.
    /// </summary>
    ValidationFailed = 2,

    /// <summary>
    ///     ISE003: Pattern match failed at current position.
    /// </summary>
    PatternMismatch = 3,

    /// <summary>
    ///     ISE004: Expected literal not found at current position.
    /// </summary>
    LiteralMismatch = 4,

    /// <summary>
    ///     ISE005: Delimiter not found in remaining input.
    /// </summary>
    DelimiterNotFound = 5,

    /// <summary>
    ///     ISE006: Expected opening delimiter not found.
    /// </summary>
    ExpectedDelimiter = 6,

    /// <summary>
    ///     ISE007: Invalid size expression value (negative or overflow).
    /// </summary>
    InvalidSize = 7,

    /// <summary>
    ///     ISE008: Invalid position value (negative).
    /// </summary>
    InvalidPosition = 8,

    /// <summary>
    ///     ISE009: Maximum iteration count exceeded in repeat.
    /// </summary>
    MaxIterationsExceeded = 9,

    /// <summary>
    ///     ISE010: String encoding error.
    /// </summary>
    EncodingError = 10,

    /// <summary>
    ///     ISE011: Expected whitespace not found.
    /// </summary>
    ExpectedWhitespace = 11,

    /// <summary>
    ///     ISE012: Switch/alternative exhausted without match.
    /// </summary>
    NoAlternativeMatched = 12,

    /// <summary>
    ///     ISE013: Circular or undefined schema reference.
    /// </summary>
    InvalidSchemaReference = 13,

    /// <summary>
    ///     ISE014: Field reference resolution error.
    /// </summary>
    FieldReferenceError = 14,

    /// <summary>
    ///     ISE015: General parsing error.
    /// </summary>
    GeneralError = 15
}
