using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

internal sealed record SourceContractDiagnosticLocationMap(
    IReadOnlyDictionary<string, SourceContractDiagnosticColumnLocation> Columns)
{
    public static SourceContractDiagnosticLocationMap Empty { get; } = new(
        new Dictionary<string, SourceContractDiagnosticColumnLocation>(StringComparer.Ordinal));

    public static SourceContractDiagnosticLocationMap FromTable(CreateTableNode table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return FromColumns(table.Columns);
    }

    public static SourceContractDiagnosticLocationMap FromColumns(
        IReadOnlyList<CreateTableColumnDefinition> columns)
    {
        if (columns.Count == 0)
            return Empty;

        var result = new Dictionary<string, SourceContractDiagnosticColumnLocation>(
            columns.Count,
            StringComparer.Ordinal);

        foreach (var column in columns)
        {
            var modifierSpans = column.ReadModifierSpans.Count == 0
                ? new Dictionary<string, TextSpan>(StringComparer.Ordinal)
                : column.ReadModifierSpans.ToDictionary(
                    static entry => entry.Key,
                    static entry => entry.Value,
                    StringComparer.Ordinal);

            result[column.ColumnName] = new SourceContractDiagnosticColumnLocation(
                column.Span,
                modifierSpans);
        }

        return new SourceContractDiagnosticLocationMap(result);
    }

    public bool TryGetModifierSpan(string? columnName, string? modifierKey, out TextSpan span)
    {
        span = TextSpan.Empty;

        if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(modifierKey))
            return false;

        if (!Columns.TryGetValue(columnName, out var column))
            return false;

        return column.ModifierSpans.TryGetValue(modifierKey, out span);
    }

    public bool TryGetColumnSpan(string? columnName, out TextSpan span)
    {
        span = TextSpan.Empty;

        if (string.IsNullOrWhiteSpace(columnName))
            return false;

        if (!Columns.TryGetValue(columnName, out var column))
            return false;

        span = column.ColumnSpan;
        return !span.IsEmpty;
    }
}
