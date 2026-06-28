using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Helpers;

public static class ScriptParameterBinder
{
    public static void ValidateNoUnknownParameters(
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyCollection<string> declaredNames)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(declaredNames);

        if (parameters.Count == 0)
            return;

        var declared = new HashSet<string>(declaredNames, StringComparer.Ordinal);
        foreach (var parameter in parameters.Keys)
        {
            if (!declared.Contains(parameter))
                throw ScriptParameterBindingException.Unknown(parameter);
        }
    }

    public static T GetRequired<T>(IReadOnlyDictionary<string, object?> parameters, string name)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!parameters.TryGetValue(name, out var value))
            throw ScriptParameterBindingException.MissingRequired(name);

        ValidateNull<T>(name, value);
        return Cast<T>(name, value);
    }

    public static T GetOptional<T>(IReadOnlyDictionary<string, object?> parameters, string name, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!parameters.TryGetValue(name, out var value))
            return defaultValue;

        ValidateNull<T>(name, value);
        return Cast<T>(name, value);
    }

    public static IReadOnlyList<T> GetRequiredCollection<T>(IReadOnlyDictionary<string, object?> parameters, string name)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!parameters.TryGetValue(name, out var value))
            throw ScriptParameterBindingException.MissingRequired(name);

        if (value == null)
            throw ScriptParameterBindingException.NullNotAllowed(name, typeof(IReadOnlyList<T>));

        return CastCollection<T>(name, value);
    }

    private static T Cast<T>(string name, object? value)
    {
        try
        {
            return (T)value!;
        }
        catch (InvalidCastException ex)
        {
            throw ScriptParameterBindingException.TypeMismatch(name, typeof(T), value, ex);
        }
    }

    private static IReadOnlyList<T> CastCollection<T>(string name, object value)
    {
        if (value is IReadOnlyList<T> readOnlyList)
            return readOnlyList;

        throw ScriptParameterBindingException.TypeMismatch(
            name,
            typeof(IReadOnlyList<T>),
            value,
            new InvalidCastException());
    }

    private static void ValidateNull<T>(string name, object? value)
    {
        if (value != null)
            return;

        var parameterType = typeof(T);
        if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
            throw ScriptParameterBindingException.NullNotAllowed(name, parameterType);
    }
}
