using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv;

internal sealed record CsvSourceOptions(
    string? Path,
    bool HasHeader,
    int SkipRows,
    string Delimiter)
{
    public static CsvSourceOptions FromParameters(IReadOnlyList<object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return parameters.Count switch
        {
            0 => new CsvSourceOptions(null, false, 0, ","),
            1 => new CsvSourceOptions(RequireString(parameters[0], "path"), false, 0, ","),
            2 => new CsvSourceOptions(
                RequireString(parameters[0], "path"),
                RequireBoolean(parameters[1], "hasHeader"),
                0,
                ","),
            3 => new CsvSourceOptions(
                RequireString(parameters[0], "path"),
                RequireBoolean(parameters[1], "hasHeader"),
                RequireInt32(parameters[2], "skipRows"),
                ","),
            4 => new CsvSourceOptions(
                RequireString(parameters[0], "path"),
                RequireBoolean(parameters[1], "hasHeader"),
                RequireInt32(parameters[2], "skipRows"),
                RequireString(parameters[3], "delimiter")),
            _ => throw new ArgumentException("CSV file source accepts at most four parameters.")
        };
    }

    private static string RequireString(object? value, string parameterName)
    {
        if (value is string text)
            return text;

        throw new ArgumentException($"CSV file source parameter '{parameterName}' must be a string.");
    }

    private static bool RequireBoolean(object? value, string parameterName)
    {
        if (value is bool flag)
            return flag;

        throw new ArgumentException($"CSV file source parameter '{parameterName}' must be a boolean.");
    }

    private static int RequireInt32(object? value, string parameterName)
    {
        return value switch
        {
            int number => number,
            long number when number is >= int.MinValue and <= int.MaxValue => (int)number,
            _ => throw new ArgumentException($"CSV file source parameter '{parameterName}' must be a 32-bit integer.")
        };
    }
}

internal sealed record CsvColumnMapping(ISchemaColumn Column, int SourceIndex, int ValueIndex);
