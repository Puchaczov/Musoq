using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static class EnumSourceDescriptorContractValidator
{
    public static void Validate(
        IReadOnlyCollection<ISchemaColumn> frozenColumns,
        SourceDescriptor descriptor,
        Func<string, TextSpan> spanResolver)
    {
        ArgumentNullException.ThrowIfNull(frozenColumns);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(spanResolver);

        if (frozenColumns.Count == 0 || frozenColumns.All(static column => column.EnumType == null))
            return;

        var describedByName = new Dictionary<string, ISchemaColumn>(StringComparer.OrdinalIgnoreCase);
        foreach (var described in descriptor.Columns)
        {
            if (describedByName.TryAdd(described.ColumnName, described))
                continue;

            throw new EnumDescriptorMismatchException(
                described.ColumnName,
                spanResolver(described.ColumnName),
                "The source returned duplicate column metadata for an enum contract.");
        }

        foreach (var frozen in frozenColumns)
        {
            if (frozen.EnumType == null &&
                (!describedByName.TryGetValue(frozen.ColumnName, out var ordinary) || ordinary.EnumType == null))
            {
                continue;
            }

            if (!describedByName.TryGetValue(frozen.ColumnName, out var described) ||
                !EnumTypesMatch(frozen.EnumType, described.EnumType) ||
                frozen.ColumnType != described.ColumnType ||
                frozen.SourceReadType != described.SourceReadType)
            {
                throw new EnumDescriptorMismatchException(
                    frozen.ColumnName,
                    spanResolver(frozen.ColumnName),
                    "The source must recompile against the frozen carrier, source-read type, and fingerprint.");
            }
        }
    }

    private static bool EnumTypesMatch(EnumTypeDescriptor? left, EnumTypeDescriptor? right)
    {
        return left == null
            ? right == null
            : right != null && string.Equals(left.Fingerprint, right.Fingerprint, StringComparison.Ordinal);
    }
}
