namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     Built-in primitive type names for binary schema fields.
///     Uses .NET type naming conventions.
/// </summary>
public enum PrimitiveTypeName
{
    /// <summary>
    ///     Unsigned 8-bit integer (1 byte). Endianness not applicable.
    /// </summary>
    Byte,

    /// <summary>
    ///     Signed 8-bit integer (1 byte). Endianness not applicable.
    /// </summary>
    SByte,

    /// <summary>
    ///     Signed 16-bit integer (2 bytes). Requires endianness.
    /// </summary>
    Short,

    /// <summary>
    ///     Unsigned 16-bit integer (2 bytes). Requires endianness.
    /// </summary>
    UShort,

    /// <summary>
    ///     Signed 32-bit integer (4 bytes). Requires endianness.
    /// </summary>
    Int,

    /// <summary>
    ///     Unsigned 32-bit integer (4 bytes). Requires endianness.
    /// </summary>
    UInt,

    /// <summary>
    ///     Signed 64-bit integer (8 bytes). Requires endianness.
    /// </summary>
    Long,

    /// <summary>
    ///     Unsigned 64-bit integer (8 bytes). Requires endianness.
    /// </summary>
    ULong,

    /// <summary>
    ///     Single-precision floating point (4 bytes). Requires endianness.
    /// </summary>
    Float,

    /// <summary>
    ///     Double-precision floating point (8 bytes). Requires endianness.
    /// </summary>
    Double
}
