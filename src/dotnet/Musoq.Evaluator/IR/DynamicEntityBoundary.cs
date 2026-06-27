using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Musoq.Evaluator.IR;

/// <summary>
/// Centralizes how the IR pipeline recognizes dynamic/object entity boundaries.
/// Shape resolution, join validation, source planning, and rendering all classify
/// dynamic rows through these helpers instead of repeating reflection checks against
/// <see cref="IDynamicMetaObjectProvider"/>, <see cref="ExpandoObject"/>, and
/// string/object dictionary types.
/// </summary>
internal static class DynamicEntityBoundary
{
    /// <summary>
    /// The string/object dictionary type generated code casts dynamic sources to.
    /// </summary>
    public static Type StringObjectDictionaryType { get; } = typeof(IDictionary<string, object>);

    /// <summary>
    /// The expando type produced for switch-shaped text schema fields.
    /// </summary>
    public static Type ExpandoType { get; } = typeof(ExpandoObject);

    /// <summary>
    /// True when the entity is dynamic: a dynamic meta-object provider or a
    /// string/object dictionary that callers read by column name at runtime.
    /// </summary>
    public static bool IsDynamicEntity(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        return IsDynamicMetaObjectProvider(entityType) ||
               IsAssignableToStringObjectDictionary(entityType);
    }

    /// <summary>
    /// True when the type implements <see cref="IDynamicMetaObjectProvider"/>.
    /// </summary>
    public static bool IsDynamicMetaObjectProvider(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type);
    }

    /// <summary>
    /// True when the type implements a string/object dictionary interface and can be
    /// accessed through dictionary members at runtime.
    /// </summary>
    public static bool IsAssignableToStringObjectDictionary(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return typeof(IReadOnlyDictionary<string, object>).IsAssignableFrom(type) ||
               typeof(IDictionary<string, object>).IsAssignableFrom(type);
    }

    /// <summary>
    /// True when the type is exactly a string/object dictionary interface or its open
    /// generic instantiated with string keys and object values.
    /// </summary>
    public static bool IsStringObjectDictionaryContext(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type == typeof(IReadOnlyDictionary<string, object>) || type == typeof(IDictionary<string, object>))
            return true;

        if (!type.IsGenericType)
            return false;

        var genericType = type.GetGenericTypeDefinition();
        if (genericType != typeof(IReadOnlyDictionary<,>) && genericType != typeof(IDictionary<,>))
            return false;

        var arguments = type.GetGenericArguments();
        return arguments[0] == typeof(string) && arguments[1] == typeof(object);
    }

    /// <summary>
    /// True when the result type should be treated as a dynamic source result shape:
    /// a dynamic meta-object provider or any dictionary-backed row.
    /// </summary>
    public static bool IsDynamicResultShape(Type resultType, Func<Type, bool> implementsGenericDictionary)
    {
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentNullException.ThrowIfNull(implementsGenericDictionary);

        return IsDynamicMetaObjectProvider(resultType) ||
               typeof(IDictionary).IsAssignableFrom(resultType) ||
               implementsGenericDictionary(resultType);
    }
}
