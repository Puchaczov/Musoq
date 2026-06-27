namespace Musoq.Converter;

internal static class InMemorySourceNames
{
    public static string NormalizeSchemaName(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name cannot be null or whitespace.", nameof(schemaName));

        var normalized = schemaName.Trim();
        return normalized.StartsWith("#", StringComparison.Ordinal)
            ? normalized[1..]
            : normalized;
    }

    public static string NormalizeSourceName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("Source name cannot be null or whitespace.", nameof(sourceName));

        var normalized = sourceName.Trim();
        return normalized.EndsWith("()", StringComparison.Ordinal)
            ? normalized[..^2]
            : normalized;
    }

    public static string CreateKey(string schemaName, string sourceName)
    {
        return $"{NormalizeSchemaName(schemaName)}.{NormalizeSourceName(sourceName)}";
    }
}
