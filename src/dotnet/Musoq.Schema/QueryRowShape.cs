using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Schema;

/// <summary>
/// Immutable logical shape passed to an opt-in query-scoped row source.
/// </summary>
public sealed record QueryRowShape
{
    public QueryRowShape(IReadOnlyList<QueryRowField> fields)
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
        Fingerprint = ComputeFingerprint(copy);
    }

    public IReadOnlyList<QueryRowField> Fields { get; }

    public string Fingerprint { get; }

    private static string ComputeFingerprint(IReadOnlyList<QueryRowField> fields)
    {
        var builder = new StringBuilder();
        foreach (var field in fields)
        {
            builder.Append(field.Slot).Append('|')
                .Append(field.SourceColumnIndex).Append('|')
                .Append(field.Name).Append('|')
                .Append(field.FieldType.AssemblyQualifiedName).Append('|')
                .Append(field.IsNullable ? '1' : '0').Append('|');

            foreach (var modifier in field.ReadModifiers.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                builder.Append(modifier.Key).Append('=').Append(modifier.Value).Append(';');

            builder.Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
