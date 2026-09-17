using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Musoq.Examples.DataSources.StructuredInputs;

/// <summary>Pure, deterministic operations used by the example sources.</summary>
public static class StructuredInputOperations
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public static PatternMatchRow[] Match(
        string text,
        IReadOnlyList<PatternInput> patterns,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(patterns);
        var rows = new List<PatternMatchRow>();

        for (var patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pattern = patterns[patternIndex];
            if (string.IsNullOrWhiteSpace(pattern.Id))
                throw new ArgumentException("Every pattern requires a non-empty Id.", nameof(patterns));
            if (string.IsNullOrEmpty(pattern.Pattern))
                throw new ArgumentException("Every pattern requires a non-empty Pattern.", nameof(patterns));

            if (string.Equals(pattern.Mode, "literal", StringComparison.OrdinalIgnoreCase))
            {
                AppendLiteralMatches(text, pattern, rows, cancellationToken);
                continue;
            }

            if (!string.Equals(pattern.Mode, "regex", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Pattern '{pattern.Id}' has unsupported mode '{pattern.Mode}'.", nameof(patterns));

            var regex = new Regex(
                pattern.Pattern,
                RegexOptions.CultureInvariant | RegexOptions.Compiled,
                RegexTimeout);
            for (var match = regex.Match(text); match.Success; match = match.NextMatch())
            {
                cancellationToken.ThrowIfCancellationRequested();
                rows.Add(new PatternMatchRow(pattern.Id, match.Value, match.Index));
            }
        }

        return rows.ToArray();
    }

    public static ConfigureRow Configure(OptionsInput options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(options.Codes);
        var codes = options.Codes.ToArray();
        return new ConfigureRow(options.Enabled, codes, options.Window.Before, options.Window.After);
    }

    public static NumberRow[] Numbers(IReadOnlyList<int> values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        var rows = new NumberRow[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows[index] = new NumberRow(values[index]);
        }

        return rows;
    }

    public static MatrixValueRow[] Matrix(
        IReadOnlyList<IReadOnlyList<int>> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        var rows = new List<MatrixValueRow>();
        for (var rowIndex = 0; rowIndex < values.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = values[rowIndex] ?? throw new ArgumentException("Matrix rows cannot be null.", nameof(values));
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rows.Add(new MatrixValueRow(row[columnIndex], rowIndex, columnIndex));
            }
        }

        return rows.ToArray();
    }

    public static WeightedRow[] Weighted(
        IReadOnlyList<WeightedInput> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        var rows = new WeightedRow[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[index];
            rows[index] = new WeightedRow(item.Value, item.Weight, item.Enabled);
        }

        return rows;
    }

    private static void AppendLiteralMatches(
        string text,
        PatternInput pattern,
        ICollection<PatternMatchRow> rows,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset <= text.Length - pattern.Pattern.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matchIndex = text.IndexOf(pattern.Pattern, offset, StringComparison.Ordinal);
            if (matchIndex < 0)
                break;

            rows.Add(new PatternMatchRow(pattern.Id, pattern.Pattern, matchIndex));
            offset = matchIndex + pattern.Pattern.Length;
        }
    }
}