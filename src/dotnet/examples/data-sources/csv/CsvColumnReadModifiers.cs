using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv;

internal static class CsvColumnReadModifiers
{
    public const string SourceIndex = $"{ColumnReadModifiers.SourcePrefix}index";
    public const string SourceName = $"{ColumnReadModifiers.SourcePrefix}name";

    public static bool IsSupported(string modifier)
    {
        return modifier is
            ColumnReadModifiers.Encoding or
            ColumnReadModifiers.Culture or
            ColumnReadModifiers.Format or
            ColumnReadModifiers.Trim or
            SourceIndex or
            SourceName;
    }
}
