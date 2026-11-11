namespace Musoq.Schema.Interpreters;

internal static class ParserFieldPath
{
    internal static string Qualify(string? nested, string field) =>
        string.IsNullOrEmpty(nested) ? field
        : string.Equals(nested, field, StringComparison.OrdinalIgnoreCase)
        || nested.StartsWith(field + ".", StringComparison.OrdinalIgnoreCase)
            ? nested
        : $"{field}.{nested}";
}
