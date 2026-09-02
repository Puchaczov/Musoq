namespace Musoq.Schema;

/// <summary>
///     Identifies how an enum descriptor entered a compilation.
/// </summary>
public enum EnumTypeOrigin : byte
{
    QueryLocal,
    NativeClr
}
