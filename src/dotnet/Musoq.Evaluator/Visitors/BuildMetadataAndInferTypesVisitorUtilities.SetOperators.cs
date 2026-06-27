using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    public static int[] CreateSetOperatorPositionIndexes(QueryNode node, string[] keys)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Length == 0)
            return Enumerable.Range(0, node.Select.Fields.Length).ToArray();

        var indexes = new int[keys.Length];

        for (var i = 0; i < keys.Length; i++)
            indexes[i] = TryGetSetOperatorFieldPosition(node, keys[i], out var position) ? position : 0;

        return indexes;
    }

    public static Type[] CreateSetOperatorPositionTypes(QueryNode node, string[] keys)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Length == 0)
            return node.Select.Fields
                .Select(static field => field.ReturnType ?? typeof(object))
                .ToArray();

        var types = new Type[keys.Length];

        for (var i = 0; i < keys.Length; i++)
        {
            if (TryGetSetOperatorFieldPosition(node, keys[i], out var position))
                types[i] = node.Select.Fields[position].ReturnType ?? typeof(object);
            else
                types[i] = typeof(object);
        }

        return types;
    }

    public static bool TryGetSetOperatorFieldPosition(QueryNode node, string key, out int position)
    {
        ArgumentNullException.ThrowIfNull(node);
        position = 0;

        for (var i = 0; i < node.Select.Fields.Length; i++)
        {
            if (!MatchesSetOperatorKey(node.Select.Fields[i], key))
                continue;

            position = i;
            return true;
        }

        return false;
    }

    private static bool MatchesSetOperatorKey(FieldNode field, string key)
    {
        if (string.Equals(field.FieldName, key, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(field.FieldName) &&
            field.FieldName.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase))
            return true;

        var expressionText = field.Expression.ToString();

        if (string.Equals(expressionText, key, StringComparison.OrdinalIgnoreCase))
            return true;

        return expressionText.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase);
    }
}
