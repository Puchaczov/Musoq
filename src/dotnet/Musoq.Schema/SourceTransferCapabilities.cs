namespace Musoq.Schema;

/// <summary>
/// Describes optional source-to-query transfer paths supported by a schema.
/// </summary>
[Flags]
public enum SourceTransferCapabilities
{
    None = 0,
    QueryScopedRows = 1,

    /// <summary>
    ///     The source can read portable logical scalar contracts directly into primitive query carriers.
    /// </summary>
    LogicalScalarReads = 2
}
