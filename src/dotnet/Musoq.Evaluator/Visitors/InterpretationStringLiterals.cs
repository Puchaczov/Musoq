namespace Musoq.Evaluator.Visitors;

internal static class InterpretationStringLiterals
{
    internal static string EscapeCSharpRegexString(string value)
    {
        return value.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    internal static string EscapeCSharpString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

}
