using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution;

internal static class CteSourceBackedFieldMap
{
    public static IReadOnlyDictionary<string, ExecutionExpression> Create(ExecutionAppendRow appendRow)
    {
        var map = new Dictionary<string, ExecutionExpression>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < appendRow.RowShape.Fields.Count && index < appendRow.Values.Count; index++)
        {
            var field = appendRow.RowShape.Fields[index];
            AddValue(map, field.Name, appendRow.Values[index].Value);
            AddValue(map, field.QualifiedName, appendRow.Values[index].Value);
            AddValue(map, appendRow.Values[index].FieldName, appendRow.Values[index].Value);
            if (field.AccessStrategy is GeneratedFieldAccess generated)
                AddValue(map, generated.FieldName, appendRow.Values[index].Value);
        }

        for (var index = 0; index < appendRow.RowShape.Contexts.Count && index < appendRow.Contexts.Count; index++)
        {
            var context = appendRow.RowShape.Contexts[index];
            AddValue(map, context.Name, appendRow.Contexts[index]);
            AddValue(map, context.QualifiedName, appendRow.Contexts[index]);
            AddValue(map, $"__context{index}", appendRow.Contexts[index]);
        }

        return map;
    }

    public static bool TryResolveFieldRead(
        ExecutionFieldRead fieldRead,
        IReadOnlyDictionary<string, ExecutionExpression> map,
        ExecutionAppendRow sourceAppend,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (fieldRead.AccessStrategy is GeneratedRowContextAccess context &&
            string.Equals(context.TypeName, sourceAppend.RowShape.TypeName, StringComparison.Ordinal) &&
            context.Index >= 0 &&
            context.Index < sourceAppend.Contexts.Count)
        {
            rewritten = sourceAppend.Contexts[context.Index];
            return true;
        }

        if (fieldRead.AccessStrategy is GeneratedFieldAccess generated &&
            map.TryGetValue(generated.FieldName, out rewritten))
        {
            return true;
        }

        if (map.TryGetValue(fieldRead.FieldName, out rewritten))
            return true;

        rewritten = null;
        return false;
    }

    private static void AddValue(
        IDictionary<string, ExecutionExpression> map,
        string key,
        ExecutionExpression value)
    {
        if (!string.IsNullOrWhiteSpace(key))
            map.TryAdd(key, value);
    }
}
