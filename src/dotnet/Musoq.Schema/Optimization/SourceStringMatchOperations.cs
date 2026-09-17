namespace Musoq.Schema.Optimization;

/// <summary>String-match shapes a source can evaluate without wildcard parsing.</summary>
[Flags]
public enum SourceStringMatchOperations
{
    /// <summary>No string-match operation is supported.</summary>
    None = 0,

    /// <summary>Exact matching is supported.</summary>
    Exact = 1 << 0,

    /// <summary>Prefix matching is supported.</summary>
    Prefix = 1 << 1,

    /// <summary>Suffix matching is supported.</summary>
    Suffix = 1 << 2,

    /// <summary>Contains matching is supported.</summary>
    Contains = 1 << 3,

    /// <summary>All version 1 string-match operations are supported.</summary>
    All = Exact | Prefix | Suffix | Contains
}
