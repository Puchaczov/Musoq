namespace Musoq.Formatter;

/// <summary>Culture-invariant casing for structural keywords.</summary>
public enum KeywordCasing
{
    /// <summary>Render structural keywords in uppercase.</summary>
    Upper,
    /// <summary>Render structural keywords in lowercase.</summary>
    Lower,
    /// <summary>Retain each keyword occurrence exactly as written.</summary>
    Preserve
}
