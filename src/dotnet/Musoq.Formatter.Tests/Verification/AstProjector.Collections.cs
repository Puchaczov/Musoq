using System;
using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Sequence<T>(IEnumerable<T>? values, Action<AstProjector, T> project)
    {
        if (values == null)
        {
            Null();
            return;
        }
        structure?.Append('[');
        foreach (var value in values)
        {
            project(this, value);
            structure?.Append(';');
        }
        structure?.Append(']');
    }

    private void Dictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue>? values,
        Action<AstProjector, TKey> key, Action<AstProjector, TValue> item)
    {
        if (values == null)
        {
            Null();
            return;
        }
        structure?.Append("dictionary[");
        foreach (var pair in values)
        {
            key(this, pair.Key);
            item(this, pair.Value);
        }
        structure?.Append(']');
    }

    private void Pair<T>(T value, Action<AstProjector, T> project) where T : struct
    {
        structure?.Append("tuple{");
        project(this, value);
        structure?.Append('}');
    }

    private void Optional<T>(T? value, Action<AstProjector, T> project) where T : struct
    {
        if (value.HasValue)
            project(this, value.Value);
        else
            Null();
    }

    private void Write(TextSpan value) => structure?.Append("source-span;");
}
