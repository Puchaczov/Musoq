using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Musoq.Schema.Optimization;

namespace Musoq.Schema;

/// <summary>
/// Immutable logical shape passed to an opt-in query-scoped row source.
/// </summary>
public sealed record QueryRowShape
{
    public QueryRowShape(IReadOnlyList<QueryRowField> fields)
        : this(fields, RowStreamReplayability.Unknown)
    {
    }

    public QueryRowShape(IReadOnlyList<QueryRowField> fields, RowStreamReplayability replayability)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var copy = fields.ToArray();
        for (var index = 0; index < copy.Length; index++)
        {
            var field = copy[index] ?? throw new ArgumentException("A query row field cannot be null.", nameof(fields));
            if (field.Slot != index)
            {
                throw new ArgumentException(
                    $"Query row slots must be contiguous and start at zero; field '{field.Name}' has slot {field.Slot}, expected {index}.",
                    nameof(fields));
            }
        }

        Fields = Array.AsReadOnly(copy);
        Replayability = replayability;
        Fingerprint = ComputeFingerprint(copy, replayability);
    }

    public IReadOnlyList<QueryRowField> Fields { get; }

    public string Fingerprint { get; }

    public RowStreamReplayability Replayability { get; }

    private static string ComputeFingerprint(
        IReadOnlyList<QueryRowField> fields,
        RowStreamReplayability replayability)
    {
        var builder = new StringBuilder();
        builder.Append("query-row-shape-v2\n");
        foreach (var field in fields)
        {
            builder.Append(field.Slot).Append('|')
                .Append(field.SourceColumnIndex).Append('|')
                .Append(field.Name).Append('|')
                .Append(field.FieldType.AssemblyQualifiedName).Append('|')
                .Append(field.SourceReadType.AssemblyQualifiedName).Append('|')
                .Append(field.EnumType?.Fingerprint ?? "-").Append('|')
                .Append(field.IsNullable ? '1' : '0').Append('|');

            if (field.Stability != ColumnStability.Stable)
                builder.Append("stability=").Append(field.Stability).Append('|');

            foreach (var modifier in field.ReadModifiers.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                builder.Append(modifier.Key).Append('=').Append(modifier.Value).Append(';');

            builder.Append('\n');
        }

        if (replayability != RowStreamReplayability.Unknown)
            builder.Append("replayability=").Append(replayability).Append('\n');

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
