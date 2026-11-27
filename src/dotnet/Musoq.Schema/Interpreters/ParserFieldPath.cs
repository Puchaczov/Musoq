namespace Musoq.Schema.Interpreters;

internal static class ParserFieldPath
{
    internal static string Qualify(string? nested, string field) =>
        string.IsNullOrEmpty(nested) ? field : $"{field}.{nested}";
}
