using System.Globalization;
using System.Text;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

internal static class CsvContractDiagnostics
{
    public static IReadOnlyList<SourceContractDiagnostic> Describe(
        IReadOnlyCollection<ISchemaColumn> columns,
        object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(parameters);

        var diagnostics = new List<SourceContractDiagnostic>();
        AddModifierDiagnostics(columns, diagnostics);
        var encoding = AddEncodingDiagnostics(columns, diagnostics);
        var options = CsvStaticFileOptions.From(parameters);

        if (options.DelimiterDiagnostic != null)
            diagnostics.Add(options.DelimiterDiagnostic);

        if (options.CanReadFile && options.DelimiterDiagnostic == null && encoding.ConflictingColumn == null)
            AddStaticFileDiagnostics(columns, options, encoding.EncodingName, diagnostics);

        return diagnostics;
    }

    private static void AddModifierDiagnostics(
        IEnumerable<ISchemaColumn> columns,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        foreach (var column in columns)
        {
            foreach (var modifier in column.ReadModifiers)
            {
                if (CsvColumnReadModifiers.IsSupported(modifier.Key))
                    continue;

                diagnostics.Add(SourceContractDiagnostic.Warning(
                    $"CSV source does not support modifier '{modifier.Key}' on column '{column.ColumnName}'.",
                    "CsvUnsupportedModifier") with
                {
                    ColumnName = column.ColumnName,
                    ModifierKey = modifier.Key
                });
            }

            AddSourceIndexDiagnostic(column, diagnostics);
        }
    }

    private static void AddSourceIndexDiagnostic(
        ISchemaColumn column,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        if (!column.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceIndex, out var sourceIndex))
            return;

        if (int.TryParse(sourceIndex, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex) &&
            parsedIndex >= 0)
            return;

        diagnostics.Add(SourceContractDiagnostic.Error(
            $"CSV source index modifier for column '{column.ColumnName}' must be a non-negative integer.",
            "CsvInvalidSourceIndex") with
        {
            ColumnName = column.ColumnName,
            ModifierKey = CsvColumnReadModifiers.SourceIndex
        });
    }

    private static EncodingDiagnostic AddEncodingDiagnostics(
        IEnumerable<ISchemaColumn> columns,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        string? requestedEncoding = null;

        foreach (var column in columns)
        {
            if (!column.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out var encoding))
                continue;

            if (requestedEncoding == null)
            {
                requestedEncoding = encoding;
                continue;
            }

            if (string.Equals(requestedEncoding, encoding, StringComparison.OrdinalIgnoreCase))
                continue;

            diagnostics.Add(SourceContractDiagnostic.Error(
                $"CSV file encoding is file-wide, but columns requested both '{requestedEncoding}' and '{encoding}'.",
                "CsvInconsistentEncoding") with
            {
                ColumnName = column.ColumnName,
                ModifierKey = ColumnReadModifiers.Encoding
            });
            return new EncodingDiagnostic(requestedEncoding, column.ColumnName);
        }

        return new EncodingDiagnostic(requestedEncoding, null);
    }

    private static void AddStaticFileDiagnostics(
        IReadOnlyCollection<ISchemaColumn> columns,
        CsvStaticFileOptions options,
        string? encodingName,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        try
        {
            using var stream = File.OpenRead(options.Path);
            using var reader = new StreamReader(stream, CsvFileSource.ResolveEncoding(encodingName), detectEncodingFromByteOrderMarks: true);
            using var records = CsvFileSource
                .ReadRecords(reader, options.Delimiter, CancellationToken.None)
                .GetEnumerator();

            SkipRecords(records, options.SkipRows);
            var header = options.HasHeader && records.MoveNext()
                ? records.Current
                : null;
            var mappings = CreateMappings(columns, options.HasHeader, header, diagnostics);

            var rowNumber = 0;
            while (records.MoveNext())
            {
                rowNumber++;
                AddShapeDiagnostics(records.Current, mappings, rowNumber, diagnostics);
            }
        }
        catch (FormatException exception)
        {
            diagnostics.Add(SourceContractDiagnostic.Error(
                exception.Message,
                "CsvMalformedQuotes"));
        }
        catch (DecoderFallbackException exception)
        {
            diagnostics.Add(SourceContractDiagnostic.Error(
                $"CSV file could not be decoded with encoding '{encodingName ?? "utf-8"}': {exception.Message}",
                "CsvInvalidEncoding"));
        }
        catch (ArgumentException exception)
        {
            diagnostics.Add(SourceContractDiagnostic.Error(
                $"CSV file could not be opened with encoding '{encodingName ?? "utf-8"}': {exception.Message}",
                "CsvInvalidEncoding"));
        }
        catch (IOException exception)
        {
            diagnostics.Add(SourceContractDiagnostic.Warning(
                $"CSV static file validation was skipped because the file could not be read: {exception.Message}",
                "CsvStaticFileValidationSkipped"));
        }
    }

    private static void SkipRecords(IEnumerator<string[]> records, int skipRows)
    {
        for (var index = 0; index < skipRows; index++)
        {
            if (!records.MoveNext())
                return;
        }
    }

    private static IReadOnlyList<CsvColumnSourceMapping> CreateMappings(
        IEnumerable<ISchemaColumn> columns,
        bool hasHeader,
        IReadOnlyList<string>? header,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        var headerIndexes = hasHeader && header != null
            ? CreateHeaderIndexes(header)
            : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var mappings = new List<CsvColumnSourceMapping>();

        foreach (var column in columns)
        {
            var sourceIndex = ResolveSourceIndex(column, hasHeader, headerIndexes, diagnostics);
            if (!sourceIndex.HasValue)
                continue;

            mappings.Add(new CsvColumnSourceMapping(
                column,
                sourceIndex.Value,
                column.ReadModifiers.ContainsKey(CsvColumnReadModifiers.SourceIndex)));
        }

        return mappings;
    }

    private static Dictionary<string, int> CreateHeaderIndexes(IReadOnlyList<string> header)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < header.Count; index++)
            indexes.TryAdd(header[index], index);

        return indexes;
    }

    private static int? ResolveSourceIndex(
        ISchemaColumn column,
        bool hasHeader,
        IReadOnlyDictionary<string, int> headerIndexes,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        if (column.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceIndex, out var explicitIndex))
        {
            return int.TryParse(explicitIndex, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex) &&
                parsedIndex >= 0
                ? parsedIndex
                : null;
        }

        if (!hasHeader)
            return column.ColumnIndex;

        var sourceName = column.ReadModifiers.TryGetValue(CsvColumnReadModifiers.SourceName, out var explicitName)
            ? explicitName
            : column.ColumnName;

        if (headerIndexes.TryGetValue(sourceName, out var headerIndex))
            return headerIndex;

        diagnostics.Add(SourceContractDiagnostic.Error(
            $"CSV header does not contain source column '{sourceName}' for table column '{column.ColumnName}'.",
            "CsvMissingSourceName") with
        {
            ColumnName = column.ColumnName,
            ModifierKey = column.ReadModifiers.ContainsKey(CsvColumnReadModifiers.SourceName)
                ? CsvColumnReadModifiers.SourceName
                : null
        });
        return null;
    }

    private static void AddShapeDiagnostics(
        IReadOnlyList<string> record,
        IEnumerable<CsvColumnSourceMapping> mappings,
        int rowNumber,
        ICollection<SourceContractDiagnostic> diagnostics)
    {
        foreach (var mapping in mappings)
        {
            if (mapping.SourceIndex < record.Count || !mapping.RequireStaticShape)
                continue;

            diagnostics.Add(SourceContractDiagnostic.Error(
                $"CSV row {rowNumber} has {record.Count} fields, but table column '{mapping.Column.ColumnName}' requires source index {mapping.SourceIndex}.",
                "CsvShapeMismatch") with
            {
                ColumnName = mapping.Column.ColumnName,
                ModifierKey = CsvColumnReadModifiers.SourceIndex
            });
            return;
        }
    }

    private sealed record EncodingDiagnostic(string? EncodingName, string? ConflictingColumn);

    private sealed record CsvColumnSourceMapping(
        ISchemaColumn Column,
        int SourceIndex,
        bool RequireStaticShape);

    private sealed record CsvStaticFileOptions(
        string Path,
        bool HasHeader,
        int SkipRows,
        char Delimiter,
        SourceContractDiagnostic? DelimiterDiagnostic)
    {
        public bool CanReadFile => !string.IsNullOrWhiteSpace(Path) && File.Exists(Path);

        public static CsvStaticFileOptions From(IReadOnlyList<object?> parameters)
        {
            var path = parameters.Count > 0 && parameters[0] is string sourcePath
                ? sourcePath
                : string.Empty;
            var hasHeader = parameters.Count > 1 && parameters[1] is bool header && header;
            var skipRows = ResolveSkipRows(parameters);
            var delimiter = parameters.Count > 3 && parameters[3] is string rawDelimiter
                ? rawDelimiter
                : ",";

            try
            {
                return new CsvStaticFileOptions(
                    path,
                    hasHeader,
                    skipRows,
                    CsvFileSource.ResolveDelimiter(delimiter),
                    null);
            }
            catch (ArgumentException exception)
            {
                return new CsvStaticFileOptions(
                    path,
                    hasHeader,
                    skipRows,
                    ',',
                    SourceContractDiagnostic.Error(exception.Message, "CsvInvalidDelimiter"));
            }
        }

        private static int ResolveSkipRows(IReadOnlyList<object?> parameters)
        {
            if (parameters.Count <= 2)
                return 0;

            return parameters[2] switch
            {
                int rows => rows,
                long rows when rows is >= int.MinValue and <= int.MaxValue => (int)rows,
                _ => 0
            };
        }
    }
}
