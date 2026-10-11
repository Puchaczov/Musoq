using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Interpreters;

/// <summary>
/// Exception thrown when interpretation schema parsing fails.
/// Its diagnostic conversion keeps parser details out of ordinary output.
/// </summary>
public class ParseException : Exception, IDiagnosticException, IVerboseDiagnosticDetails
{
    private readonly InputPositionUnit _positionUnit;
    private readonly bool _hasPosition;

    public ParseException(string message, Exception innerException)
        : base(GetSafeMessage(ParseErrorCode.GeneralError), innerException)
    {
        ErrorCode = ParseErrorCode.GeneralError;
        SchemaName = string.Empty;
        Details = message ?? string.Empty;
    }

    public ParseException(string message)
        : base(GetSafeMessage(ParseErrorCode.GeneralError))
    {
        ErrorCode = ParseErrorCode.GeneralError;
        SchemaName = string.Empty;
        Details = message ?? string.Empty;
    }

    public ParseException()
        : base(GetSafeMessage(ParseErrorCode.GeneralError))
    {
        ErrorCode = ParseErrorCode.GeneralError;
        SchemaName = string.Empty;
        Details = string.Empty;
    }

    /// <summary>Creates a structured interpretation failure using the historical character-offset default.</summary>
    public ParseException(ParseErrorCode errorCode, string schemaName, string? fieldName, int position, string details)
        : this(errorCode, schemaName, fieldName, position, details, null, InputPositionUnit.CharacterOffset)
    {
    }

    /// <summary>Creates a structured interpretation failure using the historical character-offset default.</summary>
    public ParseException(
        ParseErrorCode errorCode,
        string schemaName,
        string? fieldName,
        int position,
        string details,
        Exception innerException)
        : this(errorCode, schemaName, fieldName, position, details, innerException, InputPositionUnit.CharacterOffset)
    {
    }

    private ParseException(
        ParseErrorCode errorCode,
        string schemaName,
        string? fieldName,
        int position,
        string details,
        Exception? innerException,
        InputPositionUnit positionUnit)
        : base(GetSafeExceptionMessage(errorCode, schemaName, fieldName, position), innerException)
    {
        ErrorCode = Enum.IsDefined(errorCode) ? errorCode : ParseErrorCode.GeneralError;
        SchemaName = schemaName;
        FieldName = fieldName;
        Position = position;
        Details = details;
        _positionUnit = positionUnit;
        _hasPosition = true;
    }

    /// <summary>Creates a structured interpretation failure whose position is a byte offset.</summary>
    public static ParseException ForBytes(
        ParseErrorCode errorCode,
        string schemaName,
        string? fieldName,
        int position,
        string details,
        Exception? innerException = null)
    {
        return new ParseException(errorCode, schemaName, fieldName, position, details, innerException, InputPositionUnit.ByteOffset);
    }

    /// <summary>Creates a structured interpretation failure whose position is a character offset.</summary>
    public static ParseException ForText(
        ParseErrorCode errorCode,
        string schemaName,
        string? fieldName,
        int position,
        string details,
        Exception? innerException = null)
    {
        return new ParseException(errorCode, schemaName, fieldName, position, details, innerException, InputPositionUnit.CharacterOffset);
    }

    /// <summary>Gets the specific error code for this parse failure.</summary>
    public ParseErrorCode ErrorCode { get; }

    /// <summary>Gets the formatted error code string (ISExxxx format).</summary>
    public string FormattedErrorCode => $"ISE{(int)ErrorCode:D4}";

    /// <summary>Gets the schema name supplied to the parser.</summary>
    public string SchemaName { get; } = string.Empty;

    /// <summary>Gets the field name supplied to the parser, or null if not field-specific.</summary>
    public string? FieldName { get; }

    /// <summary>Gets the byte or character position in the input.</summary>
    public int Position { get; }

    /// <summary>Gets additional parser details for trusted diagnostics.</summary>
    public string Details { get; } = string.Empty;

    public DiagnosticCode Code => InterpretationDiagnosticCodes.FromCode((int)ErrorCode);

    public TextSpan? Span => null;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var inputLocation = CreateInputLocation();
        var arguments = new List<KeyValuePair<string, string>>
        {
            new("interpretationCode", FormattedErrorCode)
        };

        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            ErrorCatalog.GetMessage(Code),
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.Runtime,
            sourceKind: DiagnosticSourceKind.Runtime,
            arguments: arguments).WithInputLocation(inputLocation);
    }

    public string GetVerboseDiagnosticDetails()
    {
        var innerMessage = InnerException?.Message;
        if (string.IsNullOrEmpty(Details))
            return innerMessage ?? string.Empty;

        return string.IsNullOrEmpty(innerMessage)
            ? Details
            : $"{Details}{Environment.NewLine}{innerMessage}";
    }

    /// <summary>Returns a partial-result message with the ISE code and field context, excluding parser details.</summary>
    public string GetPartialInterpretationMessage()
    {
        var target = GetInterpretationTarget();
        var hasPosition = _hasPosition && Position >= 0;
        if (string.IsNullOrEmpty(target) && !hasPosition)
            return $"{FormattedErrorCode}: {GetSafeMessage(ErrorCode)}";

        var context = string.IsNullOrEmpty(target) ? "Error" : $"Error parsing {target}";
        var position = hasPosition ? $" at position {Position}" : string.Empty;
        return $"{FormattedErrorCode}: {context}{position}: {GetSafeMessage(ErrorCode)}";
    }

    private DiagnosticInputLocation? CreateInputLocation()
    {
        if (!_hasPosition || Position < 0)
            return null;

        return _positionUnit switch
        {
            InputPositionUnit.ByteOffset => new DiagnosticInputLocation(byteOffset: Position),
            InputPositionUnit.CharacterOffset => new DiagnosticInputLocation(characterOffset: Position),
            _ => null
        };
    }

    private static string GetSafeMessage(ParseErrorCode errorCode)
    {
        var safeErrorCode = Enum.IsDefined(errorCode) ? errorCode : ParseErrorCode.GeneralError;
        return ErrorCatalog.GetMessage(InterpretationDiagnosticCodes.FromCode((int)safeErrorCode));
    }

    private static string GetSafeExceptionMessage(
        ParseErrorCode errorCode,
        string schemaName,
        string? fieldName,
        int position)
    {
        var safeErrorCode = Enum.IsDefined(errorCode) ? errorCode : ParseErrorCode.GeneralError;
        var target = string.Empty;
        if (!string.IsNullOrEmpty(fieldName))
        {
            target = fieldName;
            if (!string.IsNullOrEmpty(schemaName))
                target = $"{schemaName}.{target}";
        }

        var errorContext = string.IsNullOrEmpty(target) ? "Error" : $"Error parsing {target}";
        var positionContext = position < 0 ? string.Empty : $" at position {position}";
        return $"{FormatErrorCode(safeErrorCode)}: {errorContext}{positionContext}: {GetSafeMessage(safeErrorCode)}";
    }

    private static string FormatErrorCode(ParseErrorCode errorCode)
    {
        return $"ISE{(int)errorCode:D4}";
    }

    private string GetInterpretationTarget()
    {
        if (string.IsNullOrEmpty(FieldName))
            return SchemaName;

        if (string.IsNullOrEmpty(SchemaName))
            return FieldName;

        return $"{SchemaName}.{FieldName}";
    }

    private enum InputPositionUnit
    {
        CharacterOffset,
        ByteOffset
    }
}
