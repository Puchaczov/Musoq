using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryCanonicalizeRecursiveSetOperatorKeys(
        QueryNode anchor,
        IReadOnlyList<string> keys,
        SetOperatorNode node,
        out string[] canonicalKeys)
    {
        var exportedNames = anchor.Select.Fields
            .Select(static field => field.FieldName)
            .Where(static fieldName => !string.IsNullOrWhiteSpace(fieldName))
            .ToArray();
        canonicalKeys = new string[keys.Count];

        for (var keyIndex = 0; keyIndex < keys.Count; keyIndex++)
        {
            var key = keys[keyIndex];
            var canonicalName = exportedNames.FirstOrDefault(exportedName =>
                string.Equals(exportedName, key, StringComparison.OrdinalIgnoreCase));
            if (canonicalName != null)
            {
                canonicalKeys[keyIndex] = canonicalName;
                continue;
            }

            var keySpan = GetSetOperatorKeySpan(node, keyIndex);
            if (DiagnosticContext != null)
            {
                DiagnosticContext.ReportUnknownColumn(key, exportedNames, keySpan);
                return false;
            }

            var columns = anchor.Select.Fields
                .Select((field, index) => (ISchemaColumn)new SchemaColumn(
                    field.FieldName,
                    index,
                    field.Expression.ReturnType ?? typeof(object)))
                .ToArray();
            PrepareAndThrowUnknownColumnExceptionMessage(key, columns, keySpan);
        }

        return true;
    }

    private bool ValidateSetOperatorKeys(QueryNode query, IReadOnlyList<string> keys, SetOperatorNode node)
    {
        var availableFieldNames = query.Select.Fields
            .SelectMany(field => new[] { field.FieldName, field.Expression.ToString() })
            .Where(fieldName => !string.IsNullOrWhiteSpace(fieldName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingKeyIndex = -1;
        for (var keyIndex = 0; keyIndex < keys.Count; keyIndex++)
        {
            if (!TryGetSetOperatorFieldPosition(query, keys[keyIndex], out _))
            {
                missingKeyIndex = keyIndex;
                break;
            }
        }

        if (missingKeyIndex < 0)
            return true;

        var missingKey = keys[missingKeyIndex];
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownColumn(
                missingKey,
                availableFieldNames,
                GetSetOperatorKeySpan(node, missingKeyIndex));
            return false;
        }

        throw new InvalidOperationException($"Unknown column '{missingKey}'.");
    }

    private static TextSpan GetSetOperatorKeySpan(SetOperatorNode node, int keyIndex)
    {
        return keyIndex >= 0 && keyIndex < node.KeySpans.Count && !node.KeySpans[keyIndex].IsEmpty
            ? node.KeySpans[keyIndex]
            : node.SpanOrEmpty();
    }
}
