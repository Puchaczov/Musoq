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
            return node.Select.Fields.Select(static field => field.ReturnType ?? typeof(object)).ToArray();

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
        var fields = node.Select.Fields;
        for (var i = 0; i < fields.Length; i++)
        {
            if (string.Equals(fields[i].FieldName, key, StringComparison.OrdinalIgnoreCase))
            {
                position = i;
                return true;
            }
        }

        for (var i = 0; i < fields.Length; i++)
        {
            if (!MatchesKey(fields[i], key))
                continue;

            position = i;
            return true;
        }

        position = 0;
        return false;
    }

    private static bool MatchesKey(FieldNode field, string key)
    {
        if (
            !string.IsNullOrWhiteSpace(field.FieldName)
            && field.FieldName.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        var expressionText = field.Expression.ToString();

        if (string.Equals(expressionText, key, StringComparison.OrdinalIgnoreCase))
            return true;

        return expressionText.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase);
    }
}
