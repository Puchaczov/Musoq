namespace Musoq.Schema.Interpreters;

/// <summary>Immutable parse failure data, materialized as an exception only at strict boundaries.</summary>
public sealed class ParseFailure
{
    internal static ParseFailure TolerantLiteralMismatch { get; } =
        new(ParseErrorCode.LiteralMismatch, string.Empty, null, 0, string.Empty, false);

    private readonly bool _isBinary;
    private readonly ParseException? _original;
    private readonly ParseFailure? _inner;
    private readonly Exception? _cause;

    private ParseFailure(ParseErrorCode code, string schema, string? field, int position, string details,
        bool isBinary, ParseException? original = null, ParseFailure? inner = null, Exception? cause = null)
    {
        ErrorCode = Enum.IsDefined(code) ? code : ParseErrorCode.GeneralError;
        SchemaName = schema;
        FieldName = field;
        Position = position;
        Details = details;
        _isBinary = isBinary;
        _original = original;
        _inner = inner;
        _cause = cause;
    }

    /// <summary>Gets the stable interpretation error code.</summary>
    public ParseErrorCode ErrorCode { get; }
    /// <summary>Gets the schema that reported the error.</summary>
    public string SchemaName { get; }
    /// <summary>Gets the complete nested field path.</summary>
    public string? FieldName { get; }
    /// <summary>Gets the byte or character error position.</summary>
    public int Position { get; }
    /// <summary>Gets trusted diagnostic details, excluded from ordinary partial output.</summary>
    public string Details { get; }
    /// <summary>Creates failure data using byte offsets.</summary>
    public static ParseFailure ForBytes(ParseErrorCode code, string schema, string? field, int position,
        string details, Exception? cause = null) => new(code, schema, field, position, details, true, cause: cause);
    /// <summary>Creates failure data using character offsets.</summary>
    public static ParseFailure ForText(ParseErrorCode code, string schema, string? field, int position,
        string details, Exception? cause = null) => new(code, schema, field, position, details, false, cause: cause);

    /// <summary>Retains an established interpreter exception without changing its diagnostics or identity.</summary>
    public static ParseFailure FromException(ParseException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(exception.ErrorCode, exception.SchemaName, exception.FieldName, exception.Position,
            exception.Details, false, original: exception);
    }

    internal ParseFailure WithContext(string field, bool isBinary, int? position = null) => new(ErrorCode,
        SchemaName, ParserFieldPath.Qualify(FieldName, field), position ?? Position, Details, isBinary, inner: this);

    /// <summary>Returns the established safe partial-result diagnostic.</summary>
    public string GetPartialInterpretationMessage() => _original?.GetPartialInterpretationMessage() ??
        ParseException.FormatPartialMessage(ErrorCode, SchemaName, FieldName, Position, true);

    /// <summary>Creates the established strict exception, including nested error causes.</summary>
    public ParseException ToException()
    {
        if (_original is not null)
            return _original;
        var cause = _inner?.ToException() ?? _cause;
        return _isBinary
            ? ParseException.ForBytes(ErrorCode, SchemaName, FieldName, Position, Details, cause)
            : ParseException.ForText(ErrorCode, SchemaName, FieldName, Position, Details, cause);
    }
}
