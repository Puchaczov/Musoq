namespace Musoq.Schema;

/// <summary>
///     Identifies the primitive integral carrier used by a logical enum.
/// </summary>
public enum EnumUnderlyingKind : byte
{
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64
}
