namespace Musoq.Schema.Optimization;

/// <summary>Identifies the constant string-match shape supported by a source.</summary>
public enum SourceStringMatchKind
{
    /// <summary>Requires the complete input to match the needle.</summary>
    Exact,

    /// <summary>Requires the input to begin with the needle.</summary>
    Prefix,

    /// <summary>Requires the input to end with the needle.</summary>
    Suffix,

    /// <summary>Requires the input to contain the needle.</summary>
    Contains
}
