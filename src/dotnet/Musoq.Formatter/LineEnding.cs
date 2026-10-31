namespace Musoq.Formatter;

/// <summary>Line endings for generated whitespace; protected token contents are unchanged.</summary>
public enum LineEnding
{
    /// <summary>Use the first external LF or CRLF, with LF fallback.</summary>
    Preserve,
    /// <summary>Use LF for generated line breaks.</summary>
    Lf,
    /// <summary>Use CRLF for generated line breaks.</summary>
    CrLf
}
