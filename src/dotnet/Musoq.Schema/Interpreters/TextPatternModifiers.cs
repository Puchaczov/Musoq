namespace Musoq.Schema.Interpreters;

/// <summary>Constant modifiers bound for a generated pattern field.</summary>
[Flags]
public enum TextPatternModifiers
{
    /// <summary>No modification.</summary>
    None = 0,

    /// <summary>Prefer greedy quantifiers.</summary>
    Greedy = 1,

    /// <summary>Prefer lazy quantifiers.</summary>
    Lazy = 2,

    /// <summary>Trim leading text whitespace.</summary>
    TrimStart = 4,

    /// <summary>Trim trailing text whitespace.</summary>
    TrimEnd = 8,

    /// <summary>Convert captured text to invariant lowercase.</summary>
    Lower = 16,

    /// <summary>Convert captured text to invariant uppercase.</summary>
    Upper = 32,
}
