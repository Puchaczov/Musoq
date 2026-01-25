namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Specifies the byte order for multi-byte primitive types.
/// </summary>
public enum Endianness
{
    /// <summary>
    ///     Not applicable - used for single-byte types (byte, sbyte).
    /// </summary>
    NotApplicable,

    /// <summary>
    ///     Little-endian byte order (least significant byte first).
    /// </summary>
    LittleEndian,

    /// <summary>
    ///     Big-endian byte order (most significant byte first).
    /// </summary>
    BigEndian
}
