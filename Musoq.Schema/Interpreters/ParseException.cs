#nullable enable

using System;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Exception thrown when interpretation schema parsing fails.
///     Provides detailed information about the failure location and cause.
/// </summary>
public class ParseException : Exception
{
    /// <summary>
    ///     Creates a new parse exception.
    /// </summary>
    /// <param name="errorCode">The specific error code.</param>
    /// <param name="schemaName">The name of the schema being parsed.</param>
    /// <param name="fieldName">The name of the field where the error occurred, or null if not field-specific.</param>
    /// <param name="position">The byte/character position where the error occurred.</param>
    /// <param name="details">Additional details about the error.</param>
    public ParseException(ParseErrorCode errorCode, string schemaName, string? fieldName, int position, string details)
        : base(FormatMessage(errorCode, schemaName, fieldName, position, details))
    {
        ErrorCode = errorCode;
        SchemaName = schemaName;
        FieldName = fieldName;
        Position = position;
        Details = details;
    }

    /// <summary>
    ///     Creates a new parse exception with an inner exception.
    /// </summary>
    public ParseException(ParseErrorCode errorCode, string schemaName, string? fieldName, int position, string details,
        Exception innerException)
        : base(FormatMessage(errorCode, schemaName, fieldName, position, details), innerException)
    {
        ErrorCode = errorCode;
        SchemaName = schemaName;
        FieldName = fieldName;
        Position = position;
        Details = details;
    }

    /// <summary>
    ///     Gets the specific error code for this parse failure.
    /// </summary>
    public ParseErrorCode ErrorCode { get; }

    /// <summary>
    ///     Gets the formatted error code string (ISExxxx format).
    /// </summary>
    public string FormattedErrorCode => $"ISE{(int)ErrorCode:D4}";

    /// <summary>
    ///     Gets the name of the schema being parsed when the error occurred.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     Gets the name of the field where the error occurred, or null if not field-specific.
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    ///     Gets the byte/character position in the input where the error occurred.
    /// </summary>
    public int Position { get; }

    /// <summary>
    ///     Gets additional details about the error.
    /// </summary>
    public string Details { get; }

    private static string FormatMessage(ParseErrorCode errorCode, string schemaName, string? fieldName, int position,
        string details)
    {
        var errorCodeStr = $"ISE{(int)errorCode:D4}";
        var fieldPart = fieldName != null ? $".{fieldName}" : "";
        return $"{errorCodeStr}: Error parsing {schemaName}{fieldPart} at position {position}: {details}";
    }
}
