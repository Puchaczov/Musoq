using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static class GeneratedDictionaryAccess
{
    public static object? GetValue(object? source, string key)
    {
        if (source is IReadOnlyDictionary<string, object> readOnly &&
            readOnly.TryGetValue(key, out var readOnlyValue))
        {
            return readOnlyValue;
        }

        if (source is IDictionary<string, object> dictionary &&
            dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }
}
