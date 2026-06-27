using System;
using System.Threading;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Musoq.Plugins.Attributes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSource(IEnumerable<dynamic> values) : RowSourceBase<IReadOnlyDictionary<string, object?>>
{
    private readonly IEnumerable<IReadOnlyDictionary<string, object?>> _values = values.Select(ToDictionary).ToArray();

    protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object?>> writer)
    {
        writer.Write(_values.ToList());
    }

    private static IReadOnlyDictionary<string, object?> ToDictionary(object value)
    {
        if (value is IReadOnlyDictionary<string, object?> readOnlyDictionary)
            return readOnlyDictionary;

        if (value is IDictionary<string, object?> dictionary)
            return dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);

        if (value is DynamicObject dynamicObject)
        {
            return value
                .GetType()
                .GetCustomAttributes(typeof(DynamicObjectPropertyTypeHintAttribute), inherit: true)
                .Cast<DynamicObjectPropertyTypeHintAttribute>()
                .ToDictionary(
                    hint => hint.Name,
                    hint => GetDynamicMember(dynamicObject, hint.Name));
        }

        throw new NotSupportedException(
            $"Dynamic source rows must be dictionaries. Row type '{value?.GetType().FullName ?? "<null>"}' is not supported.");
    }

    private static object? GetDynamicMember(DynamicObject value, string name)
    {
        return value.TryGetMember(new DynamicMemberBinder(name), out var result)
            ? result
            : null;
    }

    private sealed class DynamicMemberBinder(string name) : GetMemberBinder(name, ignoreCase: false)
    {
        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
        {
            return errorSuggestion ?? throw new NotSupportedException($"Dynamic member '{Name}' could not be resolved.");
        }
    }
}
