using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticSetOperatorFactService
{
    public int[] CreatePositionIndexes(QueryNode node, string[] keys)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Length == 0)
            return Enumerable.Range(0, node.Select.Fields.Length).ToArray();

        var indexes = new int[keys.Length];

        for (var i = 0; i < keys.Length; i++)
            indexes[i] = TryGetFieldPosition(node, keys[i], out var position) ? position : 0;

        return indexes;
    }

    public Type[] CreatePositionTypes(QueryNode node, string[] keys)
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
            if (TryGetFieldPosition(node, keys[i], out var position))
                types[i] = node.Select.Fields[position].ReturnType ?? typeof(object);
            else
                types[i] = typeof(object);
        }

        return types;
    }

    public bool TryGetFieldPosition(QueryNode node, string key, out int position)
    {
        ArgumentNullException.ThrowIfNull(node);
        position = 0;

        for (var i = 0; i < node.Select.Fields.Length; i++)
        {
            if (!MatchesKey(node.Select.Fields[i], key))
                continue;

            position = i;
            return true;
        }

        return false;
    }

    private static bool MatchesKey(FieldNode field, string key)
    {
        if (string.Equals(field.FieldName, key, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(field.FieldName) &&
            field.FieldName.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var expressionText = field.Expression.ToString();

        if (string.Equals(expressionText, key, StringComparison.OrdinalIgnoreCase))
            return true;

        return expressionText.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase);
    }
}
