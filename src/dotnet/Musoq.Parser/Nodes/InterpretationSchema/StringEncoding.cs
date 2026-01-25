namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Character encoding for string types in binary schemas.
/// </summary>
public enum StringEncoding
{
    /// <summary>
    ///     UTF-8 encoding (variable bytes per character, 1-4).
    ///     Size in schema refers to bytes, not characters.
    /// </summary>
    Utf8,

    /// <summary>
    ///     UTF-16 Little Endian (2 bytes per character).
    /// </summary>
    Utf16Le,

    /// <summary>
    ///     UTF-16 Big Endian (2 bytes per character).
    /// </summary>
    Utf16Be,

    /// <summary>
    ///     7-bit ASCII (1 byte per character, values 0-127).
    /// </summary>
    Ascii,

    /// <summary>
    ///     ISO-8859-1 Latin-1 (1 byte per character).
    /// </summary>
    Latin1,

    /// <summary>
    ///     IBM EBCDIC Code Page 037.
    /// </summary>
    Ebcdic
}
